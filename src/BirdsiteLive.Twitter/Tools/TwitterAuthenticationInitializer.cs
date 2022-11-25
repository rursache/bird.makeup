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
        String BearerToken { get; }
        String GuestToken { get; }
        Task EnsureAuthenticationIsInitialized();
    }

    public class TwitterAuthenticationInitializer : ITwitterAuthenticationInitializer
    {
        private readonly TwitterSettings _settings;
        private readonly ILogger<TwitterAuthenticationInitializer> _logger;
        private static bool _initialized;
        private readonly HttpClient _httpClient = new HttpClient();
        private String _token;
        public String BearerToken { 
            get { return "AAAAAAAAAAAAAAAAAAAAAPYXBAAAAAAACLXUNDekMxqa8h%2F40K4moUkGsoc%3DTYfbDKbT3jJPCEVnMYqilB28NHfOPqkca3qaAxGfsyKCs0wRbw"; }
        }
        public String GuestToken { 
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

                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.twitter.com/1.1/guest/activate.json"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer " + BearerToken); 

                        var httpResponse = await _httpClient.SendAsync(request);

                        var c = await httpResponse.Content.ReadAsStringAsync();
                        httpResponse.EnsureSuccessStatusCode();
                        var doc = JsonDocument.Parse(c);
                        _token = doc.RootElement.GetProperty("guest_token").GetString();
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