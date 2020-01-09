using MaxKagamine.Moq.HttpClient;
using Moq.Language.Flow;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Willow.Tests.Infrastructure;

namespace Willow.Tests.Infrastructure
{
    public static class MockHttpMessageHandlerExtensions
    {
        public static IReturnsResult<HttpMessageHandler> ReturnsJson<T>(this ISetup<HttpMessageHandler, Task<HttpResponseMessage>> setup, T content)
        {
            return setup.ReturnsResponse(JsonConvert.SerializeObject(content), "application/json");
        }

        public static IReturnsResult<HttpMessageHandler> ReturnsJson<T>(this ISetup<HttpMessageHandler, Task<HttpResponseMessage>> setup, HttpStatusCode statusCode, T content)
        {
            return setup.ReturnsResponse(statusCode, JsonConvert.SerializeObject(content), "application/json");
        }

        public static ISetup<HttpMessageHandler, Task<HttpResponseMessage>> SetupRequest(this DependencyServiceHttpHandler handler, HttpMethod method, string requestUrl)
        {
            return handler.HttpHandler.SetupRequest(method, $"{handler.BaseUrl}/{requestUrl}");
        }

        public static ISetup<HttpMessageHandler, Task<HttpResponseMessage>> SetupRequest(this DependencyServiceHttpHandler handler, HttpMethod method, string requestUrl, Func<HttpRequestMessage, Task<bool>> match)
        {
            return handler.HttpHandler.SetupRequest(method, $"{handler.BaseUrl}/{requestUrl}", match);
        }
    }
}
