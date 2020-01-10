using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Platform.XL.Common.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url)
        {
            return await client.GetJsonAsync<T>(url, CancellationToken.None);
        }

        public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url, CancellationToken cancellationToken) 
        {
            var responseMessage = await client.GetAsync(url, cancellationToken);
            responseMessage.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await responseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<T> GetJsonAsync<T>(this HttpClient client, Uri uri)
        {
            return await client.GetJsonAsync<T>(uri, CancellationToken.None);
        }

        public static async Task<T> GetJsonAsync<T>(this HttpClient client, Uri uri, CancellationToken cancellationToken)
        {
            var responseMessage = await client.GetAsync(uri, cancellationToken);
            responseMessage.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await responseMessage.Content.ReadAsStringAsync());
        }

        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, Uri uri, object postData, CancellationToken cancellationToken)
        {
            var content = new FormUrlEncodedContent(postData.ToDictionary(true).AsEnumerable());

            var responseMessage = await client.PostAsync(uri, content, cancellationToken);

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, object postData, CancellationToken cancellationToken)
        {
            var content = new FormUrlEncodedContent(postData.ToDictionary(true).AsEnumerable());

            var responseMessage = await client.PostAsync(url, content, cancellationToken);

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, Uri uri, object postData)
        {
            return await client.PostFormAsync(uri, postData, CancellationToken.None);
        }

        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, object postData)
        {
            return await client.PostFormAsync(url, postData, CancellationToken.None);
        }

        public static async Task<T> PostFormAsync<T>(this HttpClient client, Uri uri, object postData, CancellationToken cancellationToken)
        {
            var response = await client.PostFormAsync(uri, postData, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static async Task<T> PostFormAsync<T>(this HttpClient client, string url, object postData, CancellationToken cancellationToken)
        {
            var response = await client.PostFormAsync(url, postData, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static async Task<T> PostFormAsync<T>(this HttpClient client, Uri uri, object postData)
        {
            return await client.PostFormAsync<T>(uri, postData, CancellationToken.None);
        }

        public static async Task<T> PostFormAsync<T>(this HttpClient client, string url, object postData)
        {
            return await client.PostFormAsync<T>(url, postData, CancellationToken.None);
        }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, Uri uri, object postData, CancellationToken cancellationToken)
        {
            var content =
                new StringContent(
                    JsonConvert.SerializeObject(postData,
                        new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()}),
                    Encoding.UTF8, "application/json");

            var responseMessage = await client.PostAsync(uri, content, cancellationToken);

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object postData, CancellationToken cancellationToken)
        {
            var content =
                new StringContent(
                    JsonConvert.SerializeObject(postData,
                        new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()}),
                    Encoding.UTF8, "application/json");

            var responseMessage = await client.PostAsync(url, content, cancellationToken);

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, Uri uri, object postData)
        {
            return await client.PostJsonAsync(uri, postData, CancellationToken.None);
        }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object postData)
        {
            return await client.PostJsonAsync(url, postData, CancellationToken.None);
        }

        public static async Task<T> PostJsonAsync<T>(this HttpClient client, Uri uri, object postData, CancellationToken cancellationToken)
        {
            var response = await client.PostJsonAsync(uri, postData, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static async Task<T> PostJsonAsync<T>(this HttpClient client, string url, object postData, CancellationToken cancellationToken)
        {
            var response = await client.PostJsonAsync(url, postData, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static async Task<T> PostJsonAsync<T>(this HttpClient client, Uri uri, object postData)
        {
            return await client.PostJsonAsync<T>(uri, postData, CancellationToken.None);
        }

        public static async Task<T> PostJsonAsync<T>(this HttpClient client, string url, object postData)
        {
            return await client.PostJsonAsync<T>(url, postData, CancellationToken.None);
        }

        public static async Task<HttpResponseMessage> PutFormAsync(this HttpClient client, string url, object postData, CancellationToken cancellationToken)
        {
            var content = new FormUrlEncodedContent(postData.ToDictionary(true).AsEnumerable());

            var responseMessage = await client.PutAsync(url, content, cancellationToken);

            return responseMessage;
        }

        public static async Task<HttpResponseMessage> PutFormAsync(this HttpClient client, string url, object postData)
        {
            return await PutFormAsync(client, url, postData, CancellationToken.None);
        }

        public static async Task<T> PutFormAsync<T>(this HttpClient client, string url, object postData, CancellationToken cancellationToken)
        {
            var response = await client.PutFormAsync(url, postData, cancellationToken);
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static async Task<T> PutFormAsync<T>(this HttpClient client, string url, object postData)
        {
            return await client.PutFormAsync<T>(url, postData, CancellationToken.None);
        }
    }
}

