using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        private readonly string endpoint =
            "https://twitter.com/i/api/graphql/SAMkL5y_N9pmahSw8yy6gw/UserByScreenName?variables=%7B%22screen_name%22%3A%22elonmusk%22%2C%22withSafetyModeUserFields%22%3Atrue%7D&features=%7B%22hidden_profile_likes_enabled%22%3Afalse%2C%22hidden_profile_subscriptions_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22subscriptions_verification_info_is_identity_verified_enabled%22%3Afalse%2C%22subscriptions_verification_info_verified_since_enabled%22%3Atrue%2C%22highlights_tweets_tab_ui_enabled%22%3Atrue%2C%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%7D&fieldToggles=%7B%22withAuxiliaryUserLabels%22%3Afalse%7D";

        private static string gqlFeatures = """
        { 
          "android_graphql_skip_api_media_color_palette": false,
          "blue_business_profile_image_shape_enabled": false,
          "creator_subscriptions_subscription_count_enabled": false,
          "creator_subscriptions_tweet_preview_api_enabled": true,
          "freedom_of_speech_not_reach_fetch_enabled": false,
          "graphql_is_translatable_rweb_tweet_is_translatable_enabled": false,
          "hidden_profile_likes_enabled": false,
          "highlights_tweets_tab_ui_enabled": false,
          "interactive_text_enabled": false,
          "longform_notetweets_consumption_enabled": true,
          "longform_notetweets_inline_media_enabled": false,
          "longform_notetweets_richtext_consumption_enabled": true,
          "longform_notetweets_rich_text_read_enabled": false,
          "responsive_web_edit_tweet_api_enabled": false,
          "responsive_web_enhance_cards_enabled": false,
          "responsive_web_graphql_exclude_directive_enabled": true,
          "responsive_web_graphql_skip_user_profile_image_extensions_enabled": false,
          "responsive_web_graphql_timeline_navigation_enabled": false,
          "responsive_web_media_download_video_enabled": false,
          "responsive_web_text_conversations_enabled": false,
          "responsive_web_twitter_article_tweet_consumption_enabled": false,
          "responsive_web_twitter_blue_verified_badge_is_enabled": true,
          "rweb_lists_timeline_redesign_enabled": true,
          "spaces_2022_h2_clipping": true,
          "spaces_2022_h2_spaces_communities": true,
          "standardized_nudges_misinfo": false,
          "subscriptions_verification_info_enabled": true,
          "subscriptions_verification_info_reason_enabled": true,
          "subscriptions_verification_info_verified_since_enabled": true,
          "super_follow_badge_privacy_enabled": false,
          "super_follow_exclusive_tweet_notifications_enabled": false,
          "super_follow_tweet_api_enabled": false,
          "super_follow_user_api_enabled": false,
          "tweet_awards_web_tipping_enabled": false,
          "tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled": false,
          "tweetypie_unmention_optimization_enabled": false,
          "unified_cards_ad_metadata_container_dynamic_card_content_query_enabled": false,
          "verified_phone_label_enabled": false,
          "vibe_api_enabled": false,
          "view_counts_everywhere_api_enabled": false
        }
        """.Replace(" ", "").Replace("\n", "");

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
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Error retrieving user {Username}, Refreshing client", username);
                    await _twitterAuthenticationInitializer.RefreshClient(request);
                    return null;
                }
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
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving user {Username}", username);
                throw;
            }
            finally
            {
                _statisticsHandler.CalledApi("Twitter.User");
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

            List<long> pinnedTweets = new();
            JsonElement pinnedDoc;
            if (result.GetProperty("legacy").TryGetProperty("pinned_tweet_ids_str", out pinnedDoc))
            {
                foreach (JsonElement id in pinnedDoc.EnumerateArray())
                {
                    pinnedTweets.Add(Int64.Parse(id.GetString()));
                }
            }

            return new TwitterUser
            {
                Id = long.Parse(result.GetProperty("rest_id").GetString()),
                Acct = result.GetProperty("legacy").GetProperty("screen_name").GetString().ToLower(), 
                Name =  result.GetProperty("legacy").GetProperty("name").GetString(), //res.RootElement.GetProperty("data").GetProperty("name").GetString(),
                Description =  "", //res.RootElement.GetProperty("data").GetProperty("description").GetString(),
                Url =  "", //res.RootElement.GetProperty("data").GetProperty("url").GetString(),
                ProfileImageUrl =  result.GetProperty("legacy").GetProperty("profile_image_url_https").GetString().Replace("normal", "400x400"), 
                ProfileBackgroundImageUrl =  profileBannerURL,
                ProfileBannerURL = profileBannerURL,
                Protected = false, //res.RootElement.GetProperty("data").GetProperty("protected").GetBoolean(), 
                PinnedPosts = pinnedTweets,
                StatusCount = result.GetProperty("legacy").GetProperty("statuses_count").GetInt32()
            };

        }

        public bool IsUserApiRateLimited()
        {
            return false;
        }
    }
}