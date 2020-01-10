using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using SiemensApp.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Platform.XL.Common.Extensions;

namespace SiemensApp.Services
{
    public interface IApiTokenProvider
    {
        Task<string> GetTokenAsync(AuthenticationOptions options);
    }

    public class ApiTokenProvider : IApiTokenProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public ApiTokenProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public async Task<string> GetTokenAsync(AuthenticationOptions options)
        {
            var accessToken = await _cache.GetOrCreateAsync("SiemensApi_AccessToken",
                async (cacheEntry) =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return await GetTokenInternalAsync(options);
                });

            return accessToken;
        }

        private async Task<string> GetTokenInternalAsync(AuthenticationOptions options)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                var response = await client.PostFormAsync(options.Endpoint, new
                {
                    grant_type = "password",
                    username = options.Username,
                    password = options.Password
                });

                var data = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenSuccessResponse>(data);
                    return tokenResponse.AccessToken;
                }
                else
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenFailResponse>(data);
                    throw new AuthenticationException($"Error on acquiring access token, {tokenResponse.Error}: {tokenResponse.Details}");
                }
                
            }
        }

        private class TokenSuccessResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("toke_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public long ExpiresIn { get; set; }

            [JsonProperty("user_name")]
            public string Username { get; set; }

            [JsonProperty("user_descriptor")]
            public string UserDescriptor { get; set; }
        }

        private class TokenFailResponse
        {
            public long Id { get; set; }

            public string Error { get; set; }

            public string Details { get; set; }
        }

    }
}
