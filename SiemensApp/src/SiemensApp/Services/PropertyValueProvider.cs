using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SiemensApp.Domain;
using SiemensApp.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SiemensApp.Services
{
    public interface IPropertyValueProvider
    {
        Task<string> GetValue(string propertyName);
    }

    public class PropertyValueProvider : IPropertyValueProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PropertyValueProvider> _logger;
        private readonly BufferBlock<QueuedItem> _queue = new BufferBlock<QueuedItem>(new DataflowBlockOptions { BoundedCapacity = 100 });
        private readonly ActionBlock<QueuedItem> _propertyActionBlock;

        public PropertyValueProvider(IHttpClientFactory httpClientFactory, ILogger<PropertyValueProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _propertyActionBlock = new ActionBlock<QueuedItem>(ProcessPropertyValueItem, new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = 50});
            _queue.LinkTo(_propertyActionBlock);
        }

        private async Task ProcessPropertyValueItem(QueuedItem obj)
        {
            try
            {
                var url = "API/API/values/" + obj.PropertyName;

                using (var client = _httpClientFactory.CreateClient("Systembrowser"))
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var strResponse = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<PropertyValueResponse>>(strResponse);

                    obj.TaskSource.SetResult(data?.FirstOrDefault()?.Value?.Value);
                }
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "Error retrieving property value");
                obj.TaskSource.SetResult("");
            }
        }

        public async Task<string> GetValue(string propertyName)
        {
            _logger.LogInformation($"Getting value: " + propertyName);
            var item = new QueuedItem
            {
                PropertyName = propertyName,
                TaskSource = new TaskCompletionSource<string>()
            };

            await _queue.SendAsync(item);

            return await item.TaskSource.Task;
        }

        private class QueuedItem
        {
            public string PropertyName { get; set; }

            public TaskCompletionSource<string> TaskSource { get; set; }
        }
    }


}
