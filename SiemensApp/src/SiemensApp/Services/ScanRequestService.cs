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
using AutoMapper;
using SiemensApp.Mapping;
namespace SiemensApp.Services
{
    public interface IScanRequestService
    {
        Task<int> CreateScanRequest(ScanRequest scanRequest);
        Task<List<ScanRequest>> GetAllAsync();
        Task<List<ScanRequest>> GetAllBySiteIdAsync(Guid siteId);
        Task UpdateScanRequest(ScanRequest scanRequest);
        Task Scan(ScanRequest scanRequest);
        Task<PropertyValueResponse> GetPropertyValueAsync(string objectId, string propertyId = null);
        Task<string> ExportDataCsv();
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
        private readonly ISystemObjectService _systemObjectService;
        private readonly IServiceScopeFactory _scope;
        private readonly IMapper _mapper;
        private int ProcessingCount = 0;
        private Guid SiteId;
        public ScanRequestService(IMapper mapper, IServiceScopeFactory scope, ISystemObjectService systemObjectService, ISiteConfigurationService siteConfigurationService, IBackgroundTaskQueue taskQueue, IApplicationLifetime applicationLifetime, ILogger<ScanRequestService> logger, IHttpClientFactory httpClientFactory, IApiTokenProvider apiTokenProvider, IOptions<AppSettings> options, SiemensDbContext dbContext)
        {
            _mapper = mapper;
            _scope = scope;
            _siteConfigurationService = siteConfigurationService;
            _systemObjectService = systemObjectService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiTokenProvider = apiTokenProvider;
            _options = options.Value;
            _dbContext = dbContext;
            _taskQueue = taskQueue;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }
        public async Task<List<ScanRequest>> GetAllAsync()
        {
            return await _dbContext.ScanRequests.Select(x=> _mapper.Map<ScanRequest>(x)).ToListAsync();
        }
        public async Task<List<ScanRequest>> GetAllBySiteIdAsync(Guid siteId)
        {
            return await _dbContext.ScanRequests.Where(x => x.SiteId == siteId).Select(x => _mapper.Map<ScanRequest>(x)).ToListAsync();
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
        private async Task UpdateScanRequestInMultiThread(ScanRequest scanRequest)
        {
            using (var context = _scope.CreateScope().ServiceProvider.GetService<SiemensDbContext>())
            {
                var entity = ScanRequestEntity.MapFrom(scanRequest);
                context.Entry(entity).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            
        }
        public async Task Scan(ScanRequest scanRequest)
        {
            SiteId = scanRequest.SiteId;
            scanRequest.Id = await CreateScanRequest(scanRequest);
            var siteConfiguration = _siteConfigurationService.GetSiteConfiguration(scanRequest.SiteId);

            _taskQueue.QueueBackgroundWorkItem(async action =>
            {
                var topLevelItems = await DoWorkScan(scanRequest, siteConfiguration);
            });
            
        }
        private async Task<List<DataItem>> DoWorkScan(ScanRequest scanRequest, SiteConfiguration siteConfiguration)
        {
            var startUrl = "API/api/systembrowser";
            
            using (var client = _httpClientFactory.CreateClient())
            {
                var token = _apiTokenProvider.GetTokenAsync(AuthenticationOptions.Create(siteConfiguration)).Result;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.BaseAddress = new Uri(siteConfiguration.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                scanRequest.Status = ScanRequestStatus.Running;
                scanRequest.StartTime = DateTime.UtcNow;
                await UpdateScanRequestInMultiThread(scanRequest);

                List<DataItem> topLevelItems = null;
                try
                {
                    topLevelItems = await StartScan(client, startUrl, LinkType.Systembrowser, siteConfiguration, scanRequest);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning($"Failed to run {siteConfiguration.SiteId}. Exception Message: {ex.Message}");
                    scanRequest.Status = ScanRequestStatus.Failed;
                    scanRequest.EndTime = DateTime.UtcNow;
                    await UpdateScanRequestInMultiThread(scanRequest);
                }
                return topLevelItems;
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
        private async Task<List<DataItem>> StartScan(HttpClient client, string url, LinkType linkType, SiteConfiguration siteConfiguration, ScanRequest scanRequest)
        {
            var data = await client.GetAsync(url);
            _logger.LogInformation("Started");
            if (!data.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to invoke {statusCode} {url}", data.StatusCode, url);
                //throw new Exception($"Failed to invoke {data.StatusCode} {url}");
                return new List<DataItem>();
            }
            var strData = await data.Content.ReadAsStringAsync();
            strData = strData.Trim();
            _logger.LogInformation("URL: {url}", url);
            _logger.LogInformation("Response: {response}", strData);
            var items = linkType == LinkType.Systembrowser
                ? JsonConvert.DeserializeObject<List<DataItem>>(strData)
                : new List<DataItem> { JsonConvert.DeserializeObject<DataItem>(strData) };

            // ------- for local testing ----------

            //var items = new List<DataItem>();
            //for(int i=0; i<100; i++)
            //{
            //    items.Add(new DataItem() { SystemId = i });
            //}

            //---------------------------------

            
            foreach(var dataItem in items)
            {
                var dbEntity = new SystemObjectEntity
                {
                    ParentId = null,
                    Name = dataItem.Name,
                    Descriptor = dataItem.Descriptor,
                    Designation = dataItem.Designation,
                    ObjectId = dataItem.ObjectId,
                    SystemId = dataItem.SystemId,
                    ViewId = dataItem.ViewId,
                    SystemName = dataItem.SystemName,
                    Attributes = dataItem.Attributes?.ToString(),
                    Properties = dataItem.Properties?.ToString(),
                    FunctionProperties = dataItem.FunctionProperties?.ToString(),
                    SiteId = SiteId
                };

                _systemObjectService.CreateSystemObject(true, dbEntity).Wait();
                
                foreach (var dataItemLink in dataItem.Links)
                {
                    var lt = dataItemLink.Rel.Trim().ToLower() == "systembrowser"
                        ? LinkType.Systembrowser
                        : LinkType.Properties;

                    dataItem.ChildrenItems.AddRange(ImportRecursive(client, "api/" + dataItemLink.Href, lt, dbEntity.Id, dbEntity, siteConfiguration.MaxThreads, scanRequest));

                }
                ProcessingCount++;
                scanRequest.NumberOfPoints = ProcessingCount;
                UpdateScanRequestInMultiThread(scanRequest).Wait();
                _logger.LogInformation($"Processing Count : {ProcessingCount}");
                
            }

            scanRequest.Status = ScanRequestStatus.Completed;
            scanRequest.EndTime = DateTime.UtcNow;
            scanRequest.NumberOfPoints = ProcessingCount;
            await UpdateScanRequestInMultiThread(scanRequest);
            _logger.LogInformation($"Completed. Total Processed Count : {ProcessingCount}");
            return items;
            
        }
        private List<DataItem> ImportRecursive(HttpClient client, string url, LinkType linkType, int? parentSystemObjectId, SystemObjectEntity parentSystemObject, int MaxThreads, ScanRequest scanRequest)
        {
            var data = client.GetAsync(url).Result;
            if (!data.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to invoke {statusCode} {url}", data.StatusCode, url);
                //throw new Exception($"Failed to invoke {data.StatusCode} {url}");
                return new List<DataItem>();
            }
            var strData = data.Content.ReadAsStringAsync().Result;
            strData = strData.Trim();
            _logger.LogInformation("URL: {url}", url);
            _logger.LogInformation("Response: {response}", strData);
            var items = linkType == LinkType.Systembrowser
                ? JsonConvert.DeserializeObject<List<DataItem>>(strData)
                : new List<DataItem> { JsonConvert.DeserializeObject<DataItem>(strData) };

            if(linkType == LinkType.Properties && parentSystemObject != null)
            {
                var dataItem = items.First();
                parentSystemObject.Properties = dataItem.Properties?.ToString();
                parentSystemObject.FunctionProperties = dataItem.FunctionProperties?.ToString();
                return new List<DataItem>();
            }

            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = MaxThreads }, dataItem =>
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
                    FunctionProperties = dataItem.FunctionProperties?.ToString(),
                    SiteId = SiteId
                };

                _systemObjectService.CreateSystemObject(true, dbEntity).Wait();

                foreach (var dataItemLink in dataItem.Links)
                {
                    var lt = dataItemLink.Rel.Trim().ToLower() == "systembrowser"
                        ? LinkType.Systembrowser
                        : LinkType.Properties;

                    dataItem.ChildrenItems.AddRange(ImportRecursive(client, "api/" + dataItemLink.Href, lt, dbEntity.Id, dbEntity, 1, scanRequest));
                }

                ProcessingCount++;
                if (MaxThreads > 1)
                {
                    scanRequest.NumberOfPoints = ProcessingCount;
                    UpdateScanRequestInMultiThread(scanRequest).Wait();
                    _logger.LogInformation($"Processing Count : {ProcessingCount}");
                }
            });

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
