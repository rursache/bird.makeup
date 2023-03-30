using System;
using System.Threading;
using System.Collections.Generic;
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
        private List<HttpClient> _twitterClients = new List<HttpClient>();
        private List<String> _tokens = new List<string>();
        static Random rnd = new Random();
        private RateLimiter _rateLimiter;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
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
            //get { return "AAAAAAAAAAAAAAAAAAAAAPYXBAAAAAAACLXUNDekMxqa8h%2F40K4moUkGsoc%3DTYfbDKbT3jJPCEVnMYqilB28NHfOPqkca3qaAxGfsyKCs0wRbw"; }
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
        }
        #endregion

        private async Task GenerateBearerToken()
        {
            var httpClient = _httpClientFactory.CreateClient();
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.twitter.com/oauth2/token?grant_type=client_credentials"))
            {
                int r = rnd.Next(_apiKeys.Length);
                var (login, password) = _apiKeys[r];
                var authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{password}")));
                request.Headers.TryAddWithoutValidation("Basic", $"Bearer " + authValue); 

                var httpResponse = await httpClient.SendAsync(request);

                var c = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
                var doc = JsonDocument.Parse(c);
                var token = doc.RootElement.GetProperty("access_token").GetString();
            }
            
        }


        public async Task RefreshClient(HttpRequestMessage req)
        {
            string token = req.Headers.GetValues("x-guest-token").First();

            var i = _tokens.IndexOf(token);

            // this is prabably not thread safe but yolo
            try
            {
                _twitterClients.RemoveAt(i);
                _tokens.RemoveAt(i);
            }
            catch (IndexOutOfRangeException _)
            {
                _logger.LogError("Error refreshing twitter token");
            }

            await RefreshCred();
        }

        private async Task RefreshCred()
        {
            
            await semaphoreSlim.WaitAsync();
            try
            {
                (string bearer, string guest) = await GetCred();

                HttpClient client = _httpClientFactory.CreateClient();
                //HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer " + bearer);
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-guest-token", guest);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://twitter.com/");
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-twitter-active-user", "yes");

                _twitterClients.Add(client);
                _tokens.Add(guest);

                if (_twitterClients.Count > _targetClients)
                {
                    _twitterClients.RemoveAt(0);
                    _tokens.RemoveAt(0);
                }

            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async Task<(string, string)> GetCred()
        {
            string token;
            var httpClient = _httpClientFactory.CreateClient();
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.twitter.com/1.1/guest/activate.json"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + BearerToken); 

                var httpResponse = await httpClient.SendAsync(request);

                var c = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
                var doc = JsonDocument.Parse(c);
                token = doc.RootElement.GetProperty("guest_token").GetString();
            }

            return (BearerToken, token);

        }
        private async Task InitTwitterCredentials()
        {
            for (;;)
            {
                try
                {
                    await RefreshCred();
                    _initialized = true;
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Twitter Authentication Failed");
                    await Task.Delay(3600*1000);
                }
            }
        }

        public async Task<HttpClient> MakeHttpClient()
        {
            if (_twitterClients.Count < _targetClients)
                await RefreshCred();
            int r = rnd.Next(_twitterClients.Count);
            return _twitterClients[r];
        }
        public HttpRequestMessage MakeHttpRequest(HttpMethod m, string endpoint, bool addToken)
        {
            var request = new HttpRequestMessage(m, endpoint);
            int r = rnd.Next(_twitterClients.Count);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + BearerToken); 
            request.Headers.TryAddWithoutValidation("Referer", "https://twitter.com/");
            request.Headers.TryAddWithoutValidation("x-twitter-active-user", "yes");
            if (addToken)
                request.Headers.TryAddWithoutValidation("x-guest-token", _tokens[r]);
            //request.Headers.TryAddWithoutValidation("Referer", "https://twitter.com/");
            //request.Headers.TryAddWithoutValidation("x-twitter-active-user", "yes");
            return request;
        }
    }
}