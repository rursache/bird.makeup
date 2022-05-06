using System;
using System.Threading;
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
        String Token { get; }
        Task EnsureAuthenticationIsInitialized();
    }

    public class TwitterAuthenticationInitializer : ITwitterAuthenticationInitializer
    {
        private readonly TwitterSettings _settings;
        private readonly ILogger<TwitterAuthenticationInitializer> _logger;
        private static bool _initialized;
        private readonly HttpClient _httpClient = new HttpClient();
        private String _token;
        public String Token { 
            get { return _token; }
        }

        #region Ctor
        public TwitterAuthenticationInitializer(TwitterSettings settings, ILogger<TwitterAuthenticationInitializer> logger)
        {
            _settings = settings;
            _logger = logger;
        }
        #endregion

        public async Task EnsureAuthenticationIsInitialized()
        {
            if (_initialized) return;
           
            await InitTwitterCredentials();
        }

        private async Task InitTwitterCredentials()
        {
            for (;;)
            {
                try
                {

                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.twitter.com/oauth2/token"))
                    {
                        var base64authorization = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(_settings.ConsumerKey + ":" + _settings.ConsumerSecret));
                        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}"); 

                        request.Content = new StringContent("grant_type=client_credentials");
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"); 

                        var httpResponse = await _httpClient.SendAsync(request);

                        var c = await httpResponse.Content.ReadAsStringAsync();
                        httpResponse.EnsureSuccessStatusCode();
                        var doc = JsonDocument.Parse(c);
                        _token = doc.RootElement.GetProperty("access_token").GetString();
                    }
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
    }
}