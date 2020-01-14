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
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using Microsoft.Extensions.Options;
using SiemensApp.Infrastructure.Queue;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SqlClient;

namespace SiemensApp.Services
{
    public interface IScanRequestService
    {
        Task<int> CreateScanRequest(ScanRequest scanRequest);
        Task UpdateScanRequest(ScanRequest scanRequest);
        Task Scan(ScanRequest scanRequest);
    }
    public class ScanRequestService:IScanRequestService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger<ScanRequestService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SiemensDbContext _dbContext;
        private readonly AppSettings _options;
        private readonly IApiTokenProvider _apiTokenProvider;
        private readonly ISiteConfigurationService _siteConfigurationService;
        private readonly IServiceScopeFactory _scope;
        private int ProcessingCount = 0;
        public ScanRequestService(IServiceScopeFactory scope, ISiteConfigurationService siteConfigurationService, IBackgroundTaskQueue taskQueue, IApplicationLifetime applicationLifetime, ILogger<ScanRequestService> logger, IHttpClientFactory httpClientFactory, IApiTokenProvider apiTokenProvider, IOptions<AppSettings> options, SiemensDbContext dbContext)
        {
            _scope = scope;
            _siteConfigurationService = siteConfigurationService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiTokenProvider = apiTokenProvider;
            _options = options.Value;
            _dbContext = dbContext;
            _taskQueue = taskQueue;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }
        public async Task<int> CreateScanRequest(ScanRequest scanRequest)
        {
            var entity = ScanRequestEntity.MapFrom(scanRequest);
            _dbContext.ScanRequests.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity.Id;
        }
        public async Task UpdateScanRequest(ScanRequest scanRequest)
        {
            var entity = ScanRequestEntity.MapFrom(scanRequest);
            _dbContext.Entry(entity).State = EntityState.Modified;
            _dbContext.Entry(entity).CurrentValues.SetValues(ScanRequestEntity.MapFrom(scanRequest));
            await _dbContext.SaveChangesAsync();
        }
        private async Task UpdateScanRequest(SiemensDbContext context, ScanRequest scanRequest)
        {
            var entity = ScanRequestEntity.MapFrom(scanRequest);
            context.Entry(entity).State = EntityState.Modified;
            await context.SaveChangesAsync();
            
        }
        public async Task Scan(ScanRequest scanRequest)
        {
            var siteConfiguration = _siteConfigurationService.GetSiteConfiguration(scanRequest.SiteId);
            scanRequest.Id = await CreateScanRequest(scanRequest);
            var startUrl = "API/systembrowser";
            using (var client = _httpClientFactory.CreateClient())
            {
                var token = _apiTokenProvider.GetTokenAsync(AuthenticationOptions.Create(siteConfiguration)).Result;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.BaseAddress = new Uri(_options.SystembrowserBaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                _taskQueue.QueueBackgroundWorkItem(async action =>
                {
                    var topLevelItems = await StartScan(_scope, client, startUrl, LinkType.Systembrowser, siteConfiguration, scanRequest);
                });
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

            var totalRecords = _dbContext.SystemObjects.Count();

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

                foreach (var systemObject in _dbContext.SystemObjects.AsNoTracking())
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
        private async Task<List<DataItem>> StartScan(IServiceScopeFactory scope, HttpClient client, string url, LinkType linkType, SiteConfiguration siteConfiguration, ScanRequest scanRequest)
        {
            var context = scope.CreateScope().ServiceProvider.GetService<SiemensDbContext>();
            scanRequest.Status = ScanRequestStatus.Running;
            scanRequest.StartTime = DateTime.UtcNow;
            await UpdateScanRequest(context, scanRequest);
            var data = await client.GetAsync(url);
            _logger.LogInformation("Started");
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

            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = siteConfiguration.MaxThreads }, dataItem =>
            {

                foreach (var dataItemLink in dataItem.Links)
                {
                    var lt = dataItemLink.Rel.Trim().ToLower() == "systembrowser"
                        ? LinkType.Systembrowser
                        : LinkType.Properties;
                    dataItem.ChildrenItems.AddRange(ImportRecursive(client, dataItemLink.Href, lt));
                }
                ProcessingCount++;
                _logger.LogInformation($"Processing Count : {ProcessingCount}");
            });

            scanRequest.Status = ScanRequestStatus.Completed;
            scanRequest.EndTime = DateTime.UtcNow;
            scanRequest.NumberOfPoints = ProcessingCount;
            await UpdateScanRequest(context, scanRequest);
            _logger.LogInformation($"Completed. Total Processed Count : {ProcessingCount}");
            return items;
            
        }
        private List<DataItem> ImportRecursive(HttpClient client, string url, LinkType linkType)
        {
            var data = client.GetAsync(url).Result;
            if (!data.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to invoke {statusCode} {url}", data.StatusCode, url);
                return new List<DataItem>();
            }
            var strData = data.Content.ReadAsStringAsync().Result;
            strData = strData.Trim();
            _logger.LogInformation("URL: {url}", url);
            _logger.LogInformation("Response: {response}", strData);
            var items = linkType == LinkType.Systembrowser
                ? JsonConvert.DeserializeObject<List<DataItem>>(strData)
                : new List<DataItem> { JsonConvert.DeserializeObject<DataItem>(strData) };

            foreach (var dataItem in items)
            {
                foreach (var dataItemLink in dataItem.Links)
                {
                    var lt = dataItemLink.Rel.Trim().ToLower() == "systembrowser"
                        ? LinkType.Systembrowser
                        : LinkType.Properties;
                    dataItem.ChildrenItems.AddRange(ImportRecursive(client, dataItemLink.Href, lt));
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
