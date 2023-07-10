using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace BirdsiteLive.Twitter.Tools
{
    public interface ITwitterAuthenticationInitializer
    {
        Task<HttpClient> MakeHttpClient();
        HttpRequestMessage MakeHttpRequest(HttpMethod m, string endpoint, bool addToken);
        Task RefreshClient(HttpRequestMessage client);
    }

    public class TwitterAuthenticationInitializer : ITwitterAuthenticationInitializer
    {
        private readonly ILogger<TwitterAuthenticationInitializer> _logger;
        private static bool _initialized;
        private readonly IHttpClientFactory _httpClientFactory;
        private ConcurrentDictionary<String, String> _token2 = new ConcurrentDictionary<string, string>();
        static Random rnd = new Random();
        private RateLimiter _rateLimiter;
        private const int _targetClients = 3;
        private InstanceSettings _instanceSettings;
        private readonly (string, string)[] _apiKeys = new[]
        {
            ("IQKbtAYlXLripLGPWd0HUA", "GgDYlkSvaPxGxC4X8liwpUoqKwwr3lCADbz8A7ADU"), // iPhone
            ("3nVuSoBZnx6U4vzUxf5w", "Bcs59EFbbsdF6Sl9Ng71smgStWEGwXXKSjYvPVt7qys"), // Android
            ("CjulERsDeqhhjSme66ECg", "IQWdVyqFxghAtURHGeGiWAsmCAGmdW3WmbEx6Hck"), // iPad
            ("3rJOl1ODzm9yZy63FACdg", "5jPoQ5kQvMJFDYRNE8bQ4rHuds4xJqhvgNJM4awaE8"), // Mac
        };
        public String BearerToken { 
            get
            {
                return _instanceSettings.TwitterBearerToken;
            }
        }

        #region Ctor
        public TwitterAuthenticationInitializer(IHttpClientFactory httpClientFactory, InstanceSettings settings, ILogger<TwitterAuthenticationInitializer> logger)
        {
            _logger = logger;
            _instanceSettings = settings;
            _httpClientFactory = httpClientFactory;

            var concuOpt = new ConcurrencyLimiterOptions();
            concuOpt.PermitLimit = 1;
            _rateLimiter = new ConcurrencyLimiter(concuOpt);
        }
        #endregion

        private async Task<string> GenerateBearerToken()
        {
            var httpClient = _httpClientFactory.CreateClient();
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.twitter.com/oauth2/token?grant_type=client_credentials"))
            {
                return
                    "AAAAAAAAAAAAAAAAAAAAAFQODgEAAAAAVHTp76lzh3rFzcHbmHVvQxYYpTw%3DckAlMINMjmCwxUcaXbAN4XqJVdgMJaHqNOFgPMK0zN1qLqLQCF";
                    
                int r = rnd.Next(_apiKeys.Length);
                var (login, password) = _apiKeys[r];
                var authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{password}")));
                request.Headers.Authorization = authValue;

                var httpResponse = await httpClient.SendAsync(request);

                var c = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
                var doc = JsonDocument.Parse(c);
                var token = doc.RootElement.GetProperty("access_token").GetString();
                return token;
            }
            
        }


        public async Task RefreshClient(HttpRequestMessage req)
        {
            string token = req.Headers.GetValues("x-guest-token").First();

            _token2.TryRemove(token, out _);

            await RefreshCred();
            await Task.Delay(1000);
            await RefreshCred();
        }

        private async Task RefreshCred()
        {
            (string bearer, string guest) = await GetCred();
            _token2.TryAdd(guest, bearer);
        }

        private async Task<(string, string)> GetCred()
        {
            string token;
            var httpClient = _httpClientFactory.CreateClient();
            string bearer = await GenerateBearerToken();
            using RateLimitLease lease = await _rateLimiter.AcquireAsync(permitCount: 1);
            using var request = new HttpRequestMessage(new HttpMethod("POST"),
                "https://api.twitter.com/1.1/guest/activate.json");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + bearer);
            //request.Headers.Add("User-Agent",
            //    "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Googlebot/2.1; +http://www.google.com/bot.html) Chrome/113.0.5672.127 Safari/537.36");

            HttpResponseMessage httpResponse;
            do
            {
                httpResponse = await httpClient.SendAsync(request);

                var c = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    await Task.Delay(1000);
                httpResponse.EnsureSuccessStatusCode();
                var doc = JsonDocument.Parse(c);
                token = doc.RootElement.GetProperty("guest_token").GetString();

            } while (httpResponse.StatusCode != HttpStatusCode.OK);

            return (bearer, token);

        }

        public async Task<HttpClient> MakeHttpClient()
        {
            if (_token2.Count < _targetClients)
                await RefreshCred();
            return _httpClientFactory.CreateClient();
        }
        public HttpRequestMessage MakeHttpRequest(HttpMethod m, string endpoint, bool addToken)
        {
            var request = new HttpRequestMessage(m, endpoint);
            (string token, string bearer) = _token2.MaxBy(x => rnd.Next());
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + bearer); 
            request.Headers.TryAddWithoutValidation("Referer", "https://twitter.com/");
            request.Headers.TryAddWithoutValidation("x-twitter-active-user", "yes");
            //request.Headers.Add("User-Agent",
            //    "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Googlebot/2.1; +http://www.google.com/bot.html) Chrome/113.0.5672.127 Safari/537.36");
            if (addToken)
                request.Headers.TryAddWithoutValidation("x-guest-token", token);
            //request.Headers.TryAddWithoutValidation("Referer", "https://twitter.com/");
            //request.Headers.TryAddWithoutValidation("x-twitter-active-user", "yes");
            return request;
        }
    }
}