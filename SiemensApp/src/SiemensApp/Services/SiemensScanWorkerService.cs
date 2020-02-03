using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SiemensApp.Domain;
using SiemensApp.Entities;

namespace SiemensApp.Services
{
    public interface ISiemensScanWorkerService
    {
        Task ScanAsync(ScanRequest request, SiteConfiguration siteConfiguration);
    }

    public class SiemensScanWorkerService : ISiemensScanWorkerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IApiTokenProvider _apiTokenProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SiemensScanWorkerService> _logger;

        private BufferBlock<ScanItemTask> InputQueue;
        private TransformManyBlock<ScanItemTask, StoreItemTask> ScanBlock;
        private ActionBlock<StoreItemTask> StoreBlock;

        private Task ScanRunningTask;

        public SiemensScanWorkerService(
            IHttpClientFactory httpClientFactory,
            IApiTokenProvider apiTokenProvider,
            IServiceScopeFactory scopeFactory,
            ILogger<SiemensScanWorkerService> logger
            )
        {
            _httpClientFactory = httpClientFactory;
            _apiTokenProvider = apiTokenProvider;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ScanAsync(ScanRequest request, SiteConfiguration siteConfiguration)
        {
            if (ScanRunningTask != null)
            {
                return;
            }

            ScanRunningTask = ExecuteScanRequestAsync(request, siteConfiguration);
            await ScanRunningTask;
        }

        private async Task ExecuteScanRequestAsync(ScanRequest request, SiteConfiguration siteConfiguration)
        {
            try
            {
                _logger.LogInformation($"Start scanning scan request {request.Id} for site {siteConfiguration.SiteId}");
                request.Status = ScanRequestStatus.Running;
                request.StartTime = DateTime.UtcNow;
                await UpdateScanRequestAsync(request);

                await PerformScanAsync(request, siteConfiguration);

                _logger.LogInformation($"Finished scanning scan request {request.Id} for site {siteConfiguration.SiteId}");
                request.Status = ScanRequestStatus.Completed;
                request.EndTime = DateTime.UtcNow;
                await UpdateScanRequestAsync(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error scanning scan request {request.Id} for site {siteConfiguration.SiteId}");
                request.Status = ScanRequestStatus.Failed;
                request.EndTime = DateTime.UtcNow;
                await UpdateScanRequestAsync(request);
            }

        }

        private async Task UpdateScanRequestAsync(ScanRequest request)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SiemensDbContext>();

                UpdateScanRequestNoSave(context, request);

                await context.SaveChangesAsync();
            }
        }

        private void UpdateScanRequestNoSave(SiemensDbContext context, ScanRequest request)
        {
            var entity = ScanRequestEntity.MapFrom(request);
            var trackedEntity = context.ScanRequests.Local.FirstOrDefault(r => r.Id == entity.Id);
            if (trackedEntity != null)
            {
                context.Entry(trackedEntity).State = EntityState.Detached;
            }

            context.Entry(entity).State = EntityState.Modified;
        }

        private async Task PerformScanAsync(ScanRequest request, SiteConfiguration siteConfiguration)
        {
            var token = await _apiTokenProvider.GetTokenAsync(AuthenticationOptions.Create(siteConfiguration));

            using (var client = _httpClientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.BaseAddress = new Uri(siteConfiguration.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Initializing scanning pipeline");
                InputQueue = new BufferBlock<ScanItemTask>();

                ScanBlock = new TransformManyBlock<ScanItemTask, StoreItemTask>(async (scanTask) => await ScanItemRecursive(client, siteConfiguration.SiteId, scanTask),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = siteConfiguration.MaxThreads, SingleProducerConstrained = true }
                    );

                StoreBlock = new ActionBlock<StoreItemTask>(StoreItemAsync, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = siteConfiguration.MaxThreads, SingleProducerConstrained = true });

                InputQueue.LinkTo(ScanBlock, new DataflowLinkOptions { PropagateCompletion = true });

                ScanBlock.LinkTo(StoreBlock, new DataflowLinkOptions { PropagateCompletion = true });

                _logger.LogInformation("Sending initial scan task");
                await InputQueue.SendAsync(new ScanItemTask { Url = "API/api/systembrowser", ScanRequest = request });

                await StoreBlock.Completion;
            }
        }


        private long _elapsedStoreTotalMs;

        private async Task StoreItemAsync(StoreItemTask storeTask)
        {
            _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Storing item task started");
            if (storeTask.ParentObjectStored != null)
            {
                _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Awaiting parent item to be stored");
                await storeTask.ParentObjectStored.Task;
                _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Awaiting parent item to be stored completed");
            }

            
            using (var scope = _scopeFactory.CreateScope())
            {
                var sw = new Stopwatch();
                sw.Start();

                var context = scope.ServiceProvider.GetRequiredService<SiemensDbContext>();
                var elapsedStoreSearchParentMs = 0L;
                if (storeTask.ParentSystemObject != null)
                {
                    _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Searching for parent item id");
                    var parentItem = await context.SystemObjects
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.SiteId == storeTask.ParentSystemObject.SiteId &&
                            x.ObjectId == storeTask.ParentSystemObject.ObjectId &&
                            x.Name == storeTask.ParentSystemObject.Name &&
                            x.Designation == storeTask.ParentSystemObject.Designation);

                    storeTask.SystemObject.ParentId = parentItem?.Id;

                    elapsedStoreSearchParentMs = sw.ElapsedMilliseconds;
                }

                _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Searching for existing item");
                var existingItem = await context.SystemObjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SiteId == storeTask.SystemObject.SiteId && x.ObjectId == storeTask.SystemObject.ObjectId &&
                        x.Name == storeTask.SystemObject.Name && x.Designation == storeTask.SystemObject.Designation);

                var elapsedStoreSearchExistingMs = sw.ElapsedMilliseconds - elapsedStoreSearchParentMs;

                if (existingItem != null)
                {
                    _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Updating existing item");
                    storeTask.SystemObject.Id = existingItem.Id;
                    context.Update(storeTask.SystemObject);
                }
                else
                {
                    _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Creating new item");
                    context.Add(storeTask.SystemObject);
                }

                

                var storedPoints = Interlocked.Increment(ref _processedItems);

                if (storedPoints % 100 == 0)
                {
                    _logger.LogInformation($"Updating scan request number of points: " + _processedItems);
                    storeTask.ScanRequest.NumberOfPoints = _processedItems;
                    UpdateScanRequestNoSave(context, storeTask.ScanRequest);
                }

                _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Saving data and resolving promise");
                await context.SaveChangesAsync();
                storeTask.ObjectStored.SetResult(true);

                var elapsedStoreSaveChangesMs = sw.ElapsedMilliseconds - elapsedStoreSearchExistingMs - elapsedStoreSearchParentMs;

                sw.Stop();
                var elapsedStoreMs = Interlocked.Add(ref _elapsedStoreTotalMs, sw.ElapsedMilliseconds);
                _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Stored item: {sw.ElapsedMilliseconds}ms, Search Parent: {elapsedStoreSearchParentMs}ms, Search Existing: {elapsedStoreSearchExistingMs}ms, Save changes: {elapsedStoreSaveChangesMs}ms, Total store: {elapsedStoreMs}ms, Average: {1.0 * elapsedStoreMs / storedPoints}ms");
            }

            _logger.LogInformation($"{storeTask.SystemObject.ObjectId} - Storing item task completed");
        }

        private int _scansInProcess = 0;
        private int _processedItems = 0;
        

        private async Task<List<StoreItemTask>> ScanItemRecursive(HttpClient client, Guid siteId, ScanItemTask scanItemTask)
        {
            Interlocked.Increment(ref _scansInProcess);
            try
            {
                _logger.LogInformation($"Scanning url: " + scanItemTask.Url);

                var data = await client.GetAsync(scanItemTask.Url);
                if (!data.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to invoke {statusCode} {url}", data.StatusCode, scanItemTask.Url);
                    return new List<StoreItemTask>();
                }

                var strData = (await data.Content.ReadAsStringAsync()).Trim();

                var items = JsonConvert.DeserializeObject<List<DataItem>>(strData);

                var dbEntities = new Dictionary<SystemObjectEntity, TaskCompletionSource<bool>>();
                foreach (var dataItem in items)
                {
                    var dbEntity = new SystemObjectEntity
                    {
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
                        SiteId = siteId
                    };

                    var propertyLink = dataItem.Links.FirstOrDefault(l => l.Rel.ToLowerInvariant() != "systembrowser");
                    if (propertyLink != null)
                    {
                        var propertyData = await client.GetAsync("api/" + propertyLink.Href);
                        if (propertyData.IsSuccessStatusCode)
                        {
                            var propertyStrData = (await propertyData.Content.ReadAsStringAsync()).Trim();
                            var propertyItem = JsonConvert.DeserializeObject<DataItem>(propertyStrData);
                            dbEntity.Properties = propertyItem?.Properties?.ToString();
                            dbEntity.FunctionProperties = propertyItem?.FunctionProperties?.ToString();
                        }
                    }

                    var sytemBrowserLinks =
                        dataItem.Links.Where(l => l.Rel.ToLowerInvariant() == "systembrowser").ToList();

                    var objectStored = new TaskCompletionSource<bool>();
                    dbEntities.Add(dbEntity, objectStored);
                    foreach (var sytemBrowserLink in sytemBrowserLinks)
                    {
                        await InputQueue.SendAsync(new ScanItemTask
                        {
                            ParentSystemObject = dbEntity, Url = "api/" + sytemBrowserLink.Href,
                            ScanRequest = scanItemTask.ScanRequest, ParentObjectStored = objectStored
                        });
                    }
                }

                return dbEntities.Select(e => new StoreItemTask
                {
                    SystemObject = e.Key,
                    ObjectStored = e.Value,
                    ParentSystemObject = scanItemTask.ParentSystemObject,
                    ParentObjectStored = scanItemTask.ParentObjectStored,
                    ScanRequest = scanItemTask.ScanRequest
                }).ToList();
            }
            finally
            {
                var scansInProcess = Interlocked.Decrement(ref _scansInProcess);
                var scanQueueLength = InputQueue.Count;

                _logger.LogInformation($"Scans in process: {scansInProcess}, Queue length: {scanQueueLength}");
                if (scansInProcess == 0 && scanQueueLength == 0)
                {
                    _logger.LogInformation($"Queue is empty, last scan task finished, completing the pipeline");
                    InputQueue.Complete();
                }
            }
        }

        private class ScanItemTask
        {
            public string Url { get; set; }

            public SystemObjectEntity ParentSystemObject { get; set; }

            public TaskCompletionSource<bool> ParentObjectStored { get; set; }

            public ScanRequest ScanRequest { get; set; }
        }

        private class StoreItemTask
        {
            public SystemObjectEntity ParentSystemObject { get; set; }

            public SystemObjectEntity SystemObject { get; set; }

            public TaskCompletionSource<bool> ObjectStored { get; set; }

            public TaskCompletionSource<bool> ParentObjectStored { get; set; }

            public ScanRequest ScanRequest { get; set; }
        }
    }
}
