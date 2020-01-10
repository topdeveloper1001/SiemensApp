using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SiemensApp.Domain;
using SiemensApp.Entities;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CsvHelper;

namespace SiemensApp.Services
{
    public class ImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SiemensDbContext _context;
        private readonly IApiTokenProvider _apiTokenProvider;

        public ImportService(ILogger<ImportService> logger, IHttpClientFactory httpClientFactory, IApiTokenProvider apiTokenProvider, SiemensDbContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiTokenProvider = apiTokenProvider;
            _context = context;
        }

        public async Task Import(AuthenticationOptions authenticationOptions)
        {
            var startUrl = "API/systembrowser"; //it is not clear. I am not sure where should I get this value.
            using (var client = _httpClientFactory.CreateClient())
            {
                var token = _apiTokenProvider.GetTokenAsync(authenticationOptions).Result;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.BaseAddress = new Uri(authenticationOptions.Endpoint);   //it is not clear. I am not sure SiteConfiguration's url will be used as both of AuthenticationOptions' endpoint and baseurl.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var topLevelItems = await ImportRecursive(client, startUrl, LinkType.Systembrowser, null);
            }
        }

        private List<SystemObjectEntity> GetDbItemsRecursively(DataItem item, SystemObjectEntity parent = null)
        {
            var sObject = Map(item);
            if (parent != null)
            {
                sObject.Parent = parent;
            }

            var childrenObjects = new List<SystemObjectEntity>();
            foreach (var child in item.ChildrenItems)
            {
                childrenObjects.AddRange(GetDbItemsRecursively(child, sObject));
            }

            childrenObjects.Insert(0, sObject);

            return childrenObjects;

        }

        private SystemObjectEntity Map(DataItem item)
        {
            return new SystemObjectEntity
            {
                Name = item.Name,
                Descriptor = item.Descriptor,
                Designation = item.Designation,
                ObjectId = item.ObjectId,
                SystemId = item.SystemId,
                ViewId = item.ViewId,
                SystemName = item.SystemName,
                Attributes = item.Attributes?.ToString(),
                Properties = item.Properties?.ToString(),
                FunctionProperties = item.FunctionProperties?.ToString()
            };
        }

        public async Task<PropertyValueResponse> GetPropertyValueAsync(string objectId, string propertyId = null)
        {
            var propertyUrl = objectId;
            if (!string.IsNullOrEmpty(propertyId))
            {
                if (!propertyId.StartsWith("@"))
                {
                    propertyId = "@" + propertyId;
                }

                propertyUrl += propertyId;
            }

            var url = "API/API/values/" + propertyUrl;

            using (var client = _httpClientFactory.CreateClient("Systembrowser"))
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var strResponse = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<PropertyValueResponse>>(strResponse);

                return data.First();
            }
        }

        public async Task<List<PropertyObject>> GetObjectProperties(string objectId)
        {
            var url = "API/API/properties/" + objectId;
            using (var client = _httpClientFactory.CreateClient("Systembrowser"))
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var strResponse = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<PropertyResponse>>(strResponse);

                return data.First().Properties;
            }
        }

        public async Task<string> ExportDataCsv()
        {
            var tmpFile = Path.GetTempFileName();

            _logger.LogInformation($"Exporting data to file [{tmpFile}]");

            var recordCounter = 0;

            var totalRecords = _context.SystemObjects.Count();

            var bufferBlock = new BufferBlock<SystemObjectEntity>(new DataflowBlockOptions { BoundedCapacity = 10 });
            var transformBlock = new TransformBlock<SystemObjectEntity, CsvObject>(async (systemObject) =>
                {
                    CsvObject csvObject = null;
                    try
                    {
                        if (string.IsNullOrEmpty(systemObject.Attributes))
                        {
                            return null;
                        }

                        var attributesObject = JsonConvert.DeserializeObject<AttributesObject>(systemObject.Attributes);

                        csvObject = CsvObject.Create(systemObject, attributesObject);

                        var defaultPropertyName = attributesObject.DefaultProperty;

                        if (!string.IsNullOrEmpty(defaultPropertyName))
                        {
                            var properties = await GetObjectProperties(systemObject.ObjectId);
                            var defaultProperty = properties.FirstOrDefault(p => p.PropertyName == defaultPropertyName);
                            csvObject.UnitDescriptor = defaultProperty?.UnitDescriptor;
                        }

                        if (!string.IsNullOrEmpty(systemObject.FunctionProperties))
                        {
                            var functionProperties =
                                JsonConvert.DeserializeObject<List<string>>(systemObject.FunctionProperties);
                            if (!functionProperties.Any())
                            {
                                return csvObject;
                            }

                            var propertyValueTasks = functionProperties
                                .Select(f => new
                                { PropertyName = f, ValueTask = GetPropertyValueAsync(systemObject.ObjectId, f) })
                                .ToList();

                            await Task.WhenAll(propertyValueTasks.Select(t => t.ValueTask));

                            var propertyValues = propertyValueTasks.Select(t =>
                                new { t.PropertyName, PropertyValue = t.ValueTask.Result });
                            csvObject.FunctionProperties = JsonConvert.SerializeObject(propertyValues);
                        }

                        return csvObject;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error on processing system object");
                        return csvObject;
                    }
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 200 });



            using (StreamWriter writer = new StreamWriter(tmpFile))
            using (var csv = new CsvWriter(writer))
            {
                csv.Configuration.Delimiter = ",";
                csv.WriteHeader(typeof(CsvObject));
                csv.NextRecord();

                var writerBlock = new ActionBlock<CsvObject>(csvObject =>
                    {
                        if (csvObject == null)
                        {
                            return;
                        }
                        csv.WriteRecord(csvObject);
                        csv.NextRecord();
                    },
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

                bufferBlock.LinkTo(transformBlock, new DataflowLinkOptions { PropagateCompletion = true });
                transformBlock.LinkTo(writerBlock, new DataflowLinkOptions { PropagateCompletion = true });

                foreach (var systemObject in _context.SystemObjects.AsNoTracking())
                {
                    recordCounter++;
                    if (recordCounter % 1000 == 0)
                    {
                        _logger.LogInformation($"Processed {recordCounter}/{totalRecords} records from database");
                    }

                    await bufferBlock.SendAsync(systemObject);
                }

                bufferBlock.Complete();
                await writerBlock.Completion;
            }

            return tmpFile;
        }

        private async Task<List<DataItem>> ImportRecursive(HttpClient client, string url, LinkType linkType, int? parentSystemObjectId)
        {
            var data = await client.GetAsync(url);
            if (!data.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to invoke {statusCode} {url}", data.StatusCode, url);
                return new List<DataItem>();
            }
            var strData = await data.Content.ReadAsStringAsync();
            strData = strData.Trim();
            _logger.LogInformation("URL: {url}", url);
            _logger.LogInformation("Response: {response}", strData);
            var items = linkType == LinkType.Systembrowser
                ? JsonConvert.DeserializeObject<List<DataItem>>(strData)
                : new List<DataItem> { JsonConvert.DeserializeObject<DataItem>(strData) };

            foreach (var dataItem in items)
            {
                var dbEntity = new SystemObjectEntity
                {
                    ParentId = parentSystemObjectId,
                    Name = dataItem.Name,
                    Descriptor = dataItem.Descriptor,
                    Designation = dataItem.Designation,
                    ObjectId = dataItem.ObjectId,
                    SystemId = dataItem.SystemId,
                    ViewId = dataItem.ViewId,
                    SystemName = dataItem.SystemName,
                    Attributes = dataItem.Attributes?.ToString(),
                    Properties = dataItem.Properties?.ToString(),
                    FunctionProperties = dataItem.FunctionProperties?.ToString()
                };
                await _context.SystemObjects.AddAsync(dbEntity);
                await _context.SaveChangesAsync();
                _context.Entry(dbEntity).State = EntityState.Detached;

                foreach (var dataItemLink in dataItem.Links)
                {
                    var lt = dataItemLink.Rel.Trim().ToLower() == "systembrowser"
                        ? LinkType.Systembrowser
                        : LinkType.Properties;
                    dataItem.ChildrenItems.AddRange(await ImportRecursive(client, dataItemLink.Href, lt, dbEntity.Id));
                }
            }

            return items;
        }

        private enum LinkType
        {
            None = 0,
            Systembrowser = 1,
            Properties = 2
        }
    }
}
