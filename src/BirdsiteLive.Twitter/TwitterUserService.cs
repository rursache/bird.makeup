using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Statistics.Domain;
using BirdsiteLive.Twitter.Models;
using BirdsiteLive.Twitter.Tools;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Twitter
{
    public interface ITwitterUserService
    {
        Task<TwitterUser> GetUserAsync(string username);
        TwitterUser Extract (JsonElement result);
        bool IsUserApiRateLimited();
    }

    public class TwitterUserService : ITwitterUserService
    {
        private readonly ITwitterAuthenticationInitializer _twitterAuthenticationInitializer;
        private readonly ITwitterStatisticsHandler _statisticsHandler;
        private readonly ILogger<TwitterUserService> _logger;
        
        private readonly string endpoint = "https://twitter.com/i/api/graphql/4LB4fkCe3RDLDmOEEYtueg/UserByScreenName?variables=%7B%22screen_name%22%3A%22elonmusk%22%2C%22withSafetyModeUserFields%22%3Atrue%2C%22withSuperFollowsUserFields%22%3Atrue%7D&features=%7B%22responsive_web_twitter_blue_verified_badge_is_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22responsive_web_twitter_blue_new_verification_copy_is_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%7D";

        #region Ctor
        public TwitterUserService(ITwitterAuthenticationInitializer twitterAuthenticationInitializer, ITwitterStatisticsHandler statisticsHandler, ILogger<TwitterUserService> logger)
        {
            _twitterAuthenticationInitializer = twitterAuthenticationInitializer;
            _statisticsHandler = statisticsHandler;
            _logger = logger;
        }
        #endregion

        public async Task<TwitterUser> GetUserAsync(string username)
        {

            JsonDocument res;
            var client = await _twitterAuthenticationInitializer.MakeHttpClient();
            using var request = _twitterAuthenticationInitializer.MakeHttpRequest(new HttpMethod("GET"), endpoint.Replace("elonmusk", username), true);
            try
            {

                var httpResponse = await client.SendAsync(request);
                httpResponse.EnsureSuccessStatusCode();

                var c = await httpResponse.Content.ReadAsStringAsync();
                res = JsonDocument.Parse(c);
                var result = res.RootElement.GetProperty("data").GetProperty("user").GetProperty("result");
                return Extract(result);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new UserNotFoundException();
                //if (e.TwitterExceptionInfos.Any(x => x.Message.ToLowerInvariant().Contains("User has been suspended".ToLowerInvariant())))
                //{
                //    throw new UserHasBeenSuspendedException();
                //}
                //else if (e.TwitterExceptionInfos.Any(x => x.Message.ToLowerInvariant().Contains("User not found".ToLowerInvariant())))
                //{
                //    throw new UserNotFoundException();
                //}
                //else
                //{
                //    throw;
                //}
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error retrieving user {Username}, Refreshing client", username);
                await _twitterAuthenticationInitializer.RefreshClient(request);
                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving user {Username}", username);
                throw;
            }
            finally
            {
                _statisticsHandler.CalledUserApi();
            }

            // Expand URLs
            //var description = user.Description;
            //foreach (var descriptionUrl in user.Entities?.Description?.Urls?.OrderByDescending(x => x.URL.Length))
            //    description = description.Replace(descriptionUrl.URL, descriptionUrl.ExpandedURL);

        }

        public TwitterUser Extract(JsonElement result)
        {
            string profileBannerURL = null;
            JsonElement profileBannerURLObject;
            if (result.GetProperty("legacy").TryGetProperty("profile_banner_url", out profileBannerURLObject))
            {
                profileBannerURL = profileBannerURLObject.GetString();
            }

            return new TwitterUser
            {
                Id = long.Parse(result.GetProperty("rest_id").GetString()),
                Acct = result.GetProperty("legacy").GetProperty("screen_name").GetString(), 
                Name =  result.GetProperty("legacy").GetProperty("name").GetString(), //res.RootElement.GetProperty("data").GetProperty("name").GetString(),
                Description =  "", //res.RootElement.GetProperty("data").GetProperty("description").GetString(),
                Url =  "", //res.RootElement.GetProperty("data").GetProperty("url").GetString(),
                ProfileImageUrl =  result.GetProperty("legacy").GetProperty("profile_image_url_https").GetString().Replace("normal", "400x400"), 
                ProfileBackgroundImageUrl =  profileBannerURL,
                ProfileBannerURL = profileBannerURL,
                Protected = false, //res.RootElement.GetProperty("data").GetProperty("protected").GetBoolean(), 
            };

        }

        public bool IsUserApiRateLimited()
        {
            return false;
        }
    }
}