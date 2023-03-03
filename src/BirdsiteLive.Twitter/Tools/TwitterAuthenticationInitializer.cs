using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using System.Text.Json;

namespace BirdsiteLive.Twitter.Tools
{
    public interface ITwitterAuthenticationInitializer
    {
        Task<HttpClient> MakeHttpClient();
        HttpRequestMessage MakeHttpRequest(HttpMethod m, string endpoint);
        Task RefreshClient(HttpRequestMessage client);
    }

    public class TwitterAuthenticationInitializer : ITwitterAuthenticationInitializer
    {
        private readonly ILogger<TwitterAuthenticationInitializer> _logger;
        private static bool _initialized;
        private static System.Timers.Timer aTimer;
        private readonly IHttpClientFactory _httpClientFactory;
        private List<HttpClient> _twitterClients = new List<HttpClient>();
        private List<String> _tokens = new List<string>();
        static Random rnd = new Random();
        private const int _targetClients = 20;
        public String BearerToken { 
            get { return "AAAAAAAAAAAAAAAAAAAAAPYXBAAAAAAACLXUNDekMxqa8h%2F40K4moUkGsoc%3DTYfbDKbT3jJPCEVnMYqilB28NHfOPqkca3qaAxGfsyKCs0wRbw"; }
        }

        #region Ctor
        public TwitterAuthenticationInitializer(IHttpClientFactory httpClientFactory, ILogger<TwitterAuthenticationInitializer> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            aTimer = new System.Timers.Timer();
            aTimer.Interval = 20 * 1000; 
            aTimer.Elapsed += async (sender, e) => await RefreshCred();
            
            aTimer.Start();
        }
        #endregion


        public async Task RefreshClient(HttpRequestMessage req)
        {
            string token = req.Headers.GetValues("x-guest-token").First();

            var i = _tokens.IndexOf(token);

            // this is prabably not thread save but yolo
            try
            {
                _twitterClients.RemoveAt(i);
                _tokens.RemoveAt(i);
            }
            catch (IndexOutOfRangeException _) {}

            await RefreshCred();
        }

        private async Task RefreshCred()
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
        public HttpRequestMessage MakeHttpRequest(HttpMethod m, string endpoint)
        {
            var request = new HttpRequestMessage(m, endpoint);
            int r = rnd.Next(_twitterClients.Count);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + BearerToken); 
            request.Headers.TryAddWithoutValidation("x-guest-token", _tokens[r]);
            //request.Headers.TryAddWithoutValidation("Referer", "https://twitter.com/");
            //request.Headers.TryAddWithoutValidation("x-twitter-active-user", "yes");
            return request;
        }
    }
}