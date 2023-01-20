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
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;

namespace BirdsiteLive.Twitter
{
    public interface ITwitterTweetsService
    {
        Task<ExtractedTweet> GetTweetAsync(long statusId);
        Task<ExtractedTweet[]> GetTimelineAsync(string username, long fromTweetId = -1);
    }

    public class TwitterTweetsService : ITwitterTweetsService
    {
        private readonly ITwitterAuthenticationInitializer _twitterAuthenticationInitializer;
        private readonly ITwitterStatisticsHandler _statisticsHandler;
        private readonly ICachedTwitterUserService _twitterUserService;
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly ILogger<TwitterTweetsService> _logger;
        private HttpClient _httpClient = new HttpClient();

        #region Ctor
        public TwitterTweetsService(ITwitterAuthenticationInitializer twitterAuthenticationInitializer, ITwitterStatisticsHandler statisticsHandler, ICachedTwitterUserService twitterUserService, ITwitterUserDal twitterUserDal, ILogger<TwitterTweetsService> logger)
        {
            _twitterAuthenticationInitializer = twitterAuthenticationInitializer;
            _statisticsHandler = statisticsHandler;
            _twitterUserService = twitterUserService;
            _twitterUserDal = twitterUserDal;
            _logger = logger;
        }
        #endregion


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
                    .GetProperty("instructions").EnumerateArray().First().GetProperty("entries").EnumerateArray();

                return await Extract( timeline.Where(x => x.GetProperty("entryId").GetString() == "tweet-" + statusId).ToArray().First() );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving tweet {TweetId}", statusId);
                return null;
            }
        }

        public async Task<ExtractedTweet[]> GetTimelineAsync(string username, long fromTweetId = -1)
        {

            var client = await _twitterAuthenticationInitializer.MakeHttpClient();

            long userId;
            SyncTwitterUser user = await _twitterUserDal.GetTwitterUserAsync(username);
            if (user.TwitterUserId == default) 
            {
                var user2 = await _twitterUserService.GetUserAsync(username);
                userId = user2.Id;
                await _twitterUserDal.UpdateTwitterUserIdAsync(username, user2.Id);
            }
            else 
            {
                userId = user.TwitterUserId;
            }


            var reqURL = "https://twitter.com/i/api/graphql/s0hG9oAmWEYVBqOLJP-TBQ/UserTweetsAndReplies?variables=%7B%22userId%22%3A%22"
                 + userId + 
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
                        JsonElement userDoc = tweet.GetProperty("content").GetProperty("itemContent")
                                .GetProperty("tweet_results").GetProperty("core").GetProperty("user_results");

                        TwitterUser tweetUser = _twitterUserService.Extract(userDoc);
                        _twitterUserService.AddUser(tweetUser);
                    }
                    catch (Exception _)
                    {}

                    try 
                    {   
                        var extractedTweet = await Extract(tweet);

                        if (extractedTweet.Id == fromTweetId)
                            break;

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

        private async Task<ExtractedTweet> Extract(JsonElement tweet)
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
                OriginalAuthor = await _twitterUserService.GetUserAsync(OriginalAuthorUsername);
            }

            string creationTime = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("created_at").GetString().Replace(" +0000", "");

            JsonElement extendedEntities;
            bool hasMedia = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("extended_entities", out extendedEntities);

            JsonElement.ArrayEnumerator urls = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("entities").GetProperty("urls").EnumerateArray();
            foreach (JsonElement url in urls)
            {
                string tco = url.GetProperty("url").GetString();
                string goodUrl = url.GetProperty("expanded_url").GetString();
                MessageContent = MessageContent.Replace(tco, goodUrl);
            }
            
            List<ExtractedMedia> Media = new List<ExtractedMedia>();
            if (hasMedia) 
            {
                foreach (JsonElement media in extendedEntities.GetProperty("media").EnumerateArray())
                {
                    var type = media.GetProperty("type").GetString();
                    string url = "";
                    if (type == "video" || type == "animated_gif")
                    {
                        var bitrate = -1;
                        foreach (JsonElement v in media.GetProperty("video_info").GetProperty("variants").EnumerateArray())
                        {
                            if (v.GetProperty("content_type").GetString() !=  "video/mp4")
                                continue;
                            int vBitrate = v.GetProperty("bitrate").GetInt32();
                            if (vBitrate > bitrate)
                            {
                                bitrate = vBitrate;
                                url = v.GetProperty("url").GetString();
                            }
                        }
                    }
                    else 
                    {
                        url = media.GetProperty("media_url_https").GetString();
                    }
                    var m = new ExtractedMedia
                    {
                        MediaType = GetMediaType(type, media.GetProperty("media_url_https").GetString()),
                        Url = url,
                    };
                    Media.Add(m);

                    MessageContent = MessageContent.Replace(media.GetProperty("url").GetString(), "");
                }
            }

            bool isQuoteTweet = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("is_quote_status").GetBoolean();

            if (isQuoteTweet) 
            {

                string quoteTweetLink = tweet.GetProperty("content").GetProperty("itemContent")
                        .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                        .GetProperty("quoted_status_permalink").GetProperty("expanded").GetString();
                MessageContent = MessageContent + "\n" + quoteTweetLink;
            }
            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweet.GetProperty("sortIndex").GetString()),
                InReplyToStatusId = inReplyToPostId,
                InReplyToAccount = inReplyToUser,
                MessageContent = MessageContent.Trim(),
                CreatedAt = DateTime.ParseExact(creationTime, "ddd MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture),
                IsReply = isReply,
                IsThread = false,
                IsRetweet = isRetweet,
                Media = Media.Count() == 0 ? null : Media.ToArray(),
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
