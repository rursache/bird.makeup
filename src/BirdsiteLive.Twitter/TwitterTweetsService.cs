using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Statistics.Domain;
using BirdsiteLive.Twitter.Models;
using BirdsiteLive.Twitter.Tools;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BirdsiteLive.Twitter
{
    public interface ITwitterTweetsService
    {
        ExtractedTweet GetTweet(long statusId);
        ExtractedTweet[] GetTimeline(string username, int nberTweets, long fromTweetId = -1);
    }

    public class TwitterTweetsService : ITwitterTweetsService
    {
        private readonly ITwitterAuthenticationInitializer _twitterAuthenticationInitializer;
        private readonly ITwitterStatisticsHandler _statisticsHandler;
        private readonly ITwitterUserService _twitterUserService;
        private readonly ILogger<TwitterTweetsService> _logger;
        private HttpClient _httpClient = new HttpClient();

        #region Ctor
        public TwitterTweetsService(ITwitterAuthenticationInitializer twitterAuthenticationInitializer, ITwitterStatisticsHandler statisticsHandler, ITwitterUserService twitterUserService, ILogger<TwitterTweetsService> logger)
        {
            _twitterAuthenticationInitializer = twitterAuthenticationInitializer;
            _statisticsHandler = statisticsHandler;
            _twitterUserService = twitterUserService;
            _logger = logger;
        }
        #endregion


        public ExtractedTweet GetTweet(long statusId)
        {
            return GetTweetAsync(statusId).Result;
        }
        public async Task<ExtractedTweet> GetTweetAsync(long statusId)
        {


            var client = await _twitterAuthenticationInitializer.MakeHttpClient();


            string reqURL = "https://twitter.com/i/api/graphql/BoHLKeBvibdYDiJON1oqTg/TweetDetail?variables=%7B%22focalTweetId%22%3A%22"
            + statusId + "%22%2C%22referrer%22%3A%22profile%22%2C%22rux_context%22%3A%22HHwWgICypZb4saYsAAAA%22%2C%22with_rux_injections%22%3Atrue%2C%22includePromotedContent%22%3Atrue%2C%22withCommunity%22%3Atrue%2C%22withQuickPromoteEligibilityTweetFields%22%3Atrue%2C%22withBirdwatchNotes%22%3Afalse%2C%22withSuperFollowsUserFields%22%3Atrue%2C%22withDownvotePerspective%22%3Afalse%2C%22withReactionsMetadata%22%3Afalse%2C%22withReactionsPerspective%22%3Afalse%2C%22withSuperFollowsTweetFields%22%3Atrue%2C%22withVoice%22%3Atrue%2C%22withV2Timeline%22%3Atrue%7D&features=%7B%22responsive_web_twitter_blue_verified_badge_is_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22unified_cards_ad_metadata_container_dynamic_card_content_query_enabled%22%3Atrue%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_uc_gql_enabled%22%3Atrue%2C%22vibe_api_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Afalse%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Afalse%2C%22interactive_text_enabled%22%3Atrue%2C%22responsive_web_text_conversations_enabled%22%3Afalse%2C%22responsive_web_enhance_cards_enabled%22%3Atrue%7D";

            try
            {
                JsonDocument tweet;
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL))
                {
                    var httpResponse = await client.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();
                    var c = await httpResponse.Content.ReadAsStringAsync();
                    tweet = JsonDocument.Parse(c);
                }


                var timeline = tweet.RootElement.GetProperty("data").GetProperty("threaded_conversation_with_injections_v2")
                    .GetProperty("instructions").GetProperty("entries").EnumerateArray();

                return Extract( timeline.First() );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving tweet {TweetId}", statusId);
                return null;
            }
        }

        public ExtractedTweet[] GetTimeline(string username, int nberTweets, long fromTweetId = -1)
        {
            return GetTimelineAsync(username, nberTweets, fromTweetId).Result;
        }
        public async Task<ExtractedTweet[]> GetTimelineAsync(string username, int nberTweets, long fromTweetId = -1)
        {
            if (nberTweets < 5)
                nberTweets = 5;

            if (nberTweets > 100)
                nberTweets = 100;

            var client = await _twitterAuthenticationInitializer.MakeHttpClient();

            var user = _twitterUserService.GetUser(username);
            if (user == null || user.Protected) return new ExtractedTweet[0];


            var reqURL = "https://twitter.com/i/api/graphql/s0hG9oAmWEYVBqOLJP-TBQ/UserTweetsAndReplies?variables=%7B%22userId%22%3A%22"
                 + user.Id + 
                "%22%2C%22count%22%3A40%2C%22includePromotedContent%22%3Atrue%2C%22withQuickPromoteEligibilityTweetFields%22%3Atrue%2C%22withSuperFollowsUserFields%22%3Atrue%2C%22withDownvotePerspective%22%3Afalse%2C%22withReactionsMetadata%22%3Afalse%2C%22withReactionsPerspective%22%3Afalse%2C%22withSuperFollowsTweetFields%22%3Atrue%2C%22withVoice%22%3Atrue%2C%22withV2Timeline%22%3Atrue%7D&features=%7B%22responsive_web_twitter_blue_verified_badge_is_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22unified_cards_ad_metadata_container_dynamic_card_content_query_enabled%22%3Atrue%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_uc_gql_enabled%22%3Atrue%2C%22vibe_api_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Afalse%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Afalse%2C%22interactive_text_enabled%22%3Atrue%2C%22responsive_web_text_conversations_enabled%22%3Afalse%2C%22responsive_web_enhance_cards_enabled%22%3Atrue%7D";
            JsonDocument results;
            List<ExtractedTweet> extractedTweets = new List<ExtractedTweet>();
            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL))
                {

                    var httpResponse = await client.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();
                    var c = await httpResponse.Content.ReadAsStringAsync();
                    results = JsonDocument.Parse(c);
                }

                _statisticsHandler.CalledTweetApi();
                if (results == null) return null; //TODO: test this
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving timeline ", username);
                return null;
            }

            var timeline = results.RootElement.GetProperty("data").GetProperty("user").GetProperty("result")
                .GetProperty("timeline_v2").GetProperty("timeline").GetProperty("instructions").EnumerateArray();

            foreach (JsonElement timelineElement in timeline) 
            {
                if (timelineElement.GetProperty("type").GetString() != "TimelineAddEntries")
                    continue;

                
                foreach (JsonElement tweet in timelineElement.GetProperty("entries").EnumerateArray())
                {
                    if (tweet.GetProperty("content").GetProperty("entryType").GetString() != "TimelineTimelineItem")
                        continue;
                    
                    try 
                    {   
                        var extractedTweet = Extract(tweet);
                        extractedTweets.Add(extractedTweet);

                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Tried getting timeline from user " + username + ", but got error: \n" + e.Message + e.StackTrace + e.Source
                            + JsonObject.Create(tweet).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                    }
                }
            }

            return extractedTweets.ToArray();
        }

        private ExtractedTweet Extract(JsonElement tweet)
        {

            JsonElement retweet;
            TwitterUser OriginalAuthor;
            JsonElement inReplyToPostIdElement;
            JsonElement inReplyToUserElement;
            string inReplyToUser = null;
            long? inReplyToPostId = null;

            bool isReply = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("in_reply_to_status_id_str", out inReplyToPostIdElement);
            tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("in_reply_to_screen_name", out inReplyToUserElement);
            if (isReply) 
            {
                inReplyToPostId = Int64.Parse(inReplyToPostIdElement.GetString());
                inReplyToUser = inReplyToUserElement.GetString();
            }
            bool isRetweet = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("retweeted_status_result", out retweet);
            string MessageContent;
            if (!isRetweet)
            {
                MessageContent = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("full_text").GetString();
                OriginalAuthor = null;
            }
            else 
            {
                MessageContent = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("legacy").GetProperty("full_text").GetString();
                string OriginalAuthorUsername = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("core").GetProperty("user_results").GetProperty("result")
                    .GetProperty("legacy").GetProperty("screen_name").GetString();
                OriginalAuthor = _twitterUserService.GetUser(OriginalAuthorUsername);
            }

            string creationTime = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("created_at").GetString().Replace(" +0000", "");
            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweet.GetProperty("sortIndex").GetString()),
                InReplyToStatusId = inReplyToPostId,
                InReplyToAccount = inReplyToUser,
                MessageContent = MessageContent,
                CreatedAt = DateTime.ParseExact(creationTime, "ddd MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture),
                IsReply = isReply,
                IsThread = false,
                IsRetweet = isRetweet,
                Media = null,
                RetweetUrl = "https://t.co/123",
                OriginalAuthor = OriginalAuthor,
            };
       
            return extractedTweet;
         
        }
        private string GetMediaType(string mediaType, string mediaUrl)
        {
            switch (mediaType)
            {
                case "photo":
                    var pExt = Path.GetExtension(mediaUrl);
                    switch (pExt)
                    {
                        case ".jpg":
                        case ".jpeg":
                            return "image/jpeg";
                        case ".png":
                            return "image/png";
                    }
                    return null;

                case "animated_gif":
                    var vExt = Path.GetExtension(mediaUrl);
                    switch (vExt)
                    {
                        case ".gif":
                            return "image/gif";
                        case ".mp4":
                            return "video/mp4";
                    }
                    return "image/gif";
                case "video":
                    return "video/mp4";
            }
            return null;
        }
    }
}
