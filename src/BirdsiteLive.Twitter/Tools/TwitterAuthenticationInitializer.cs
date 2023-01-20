using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BirdsiteLive.Twitter.Tools
{
    public interface ITwitterAuthenticationInitializer
    {
        Task EnsureAuthenticationIsInitialized();
        Task<HttpClient> MakeHttpClient();
    }

    public class TwitterAuthenticationInitializer : ITwitterAuthenticationInitializer
    {
        private readonly ILogger<TwitterAuthenticationInitializer> _logger;
        private static bool _initialized;
        private static System.Timers.Timer aTimer;
        private readonly HttpClient _httpClient = new HttpClient();
        private List<HttpClient> _twitterClients = new List<HttpClient>();
        private List<String> _tokens = new List<string>();
        static Random rnd = new Random();
        public String BearerToken { 
            get { return "AAAAAAAAAAAAAAAAAAAAAPYXBAAAAAAACLXUNDekMxqa8h%2F40K4moUkGsoc%3DTYfbDKbT3jJPCEVnMYqilB28NHfOPqkca3qaAxGfsyKCs0wRbw"; }
        }

        #region Ctor
        public TwitterAuthenticationInitializer(ILogger<TwitterAuthenticationInitializer> logger)
        {
            _logger = logger;

            aTimer = new System.Timers.Timer();
            aTimer.Interval = 45 * 1000; 
            aTimer.Elapsed += async (sender, e) => await RefreshCred();
            
            aTimer.Start();
        }
        #endregion

        public async Task EnsureAuthenticationIsInitialized()
        {
            if (_initialized) return;
           
            await InitTwitterCredentials();
        }

        private async Task RefreshCred()
        {
            (string bearer, string guest) = await GetCred();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer " + bearer); 
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-guest-token", guest);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://twitter.com/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-twitter-active-user", "yes");

            _twitterClients.Add(client);

            if (_twitterClients.Count > 10)
                _twitterClients.RemoveAt(0);
        }

        private async Task<(string, string)> GetCred()
        {
            string token;
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.twitter.com/1.1/guest/activate.json"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + BearerToken); 

                var httpResponse = await _httpClient.SendAsync(request);

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
            if (_twitterClients.Count < 3)
                await RefreshCred();
            int r = rnd.Next(_twitterClients.Count);
            return _twitterClients[r];
        }
    }
}