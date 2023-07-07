using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
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
        private readonly InstanceSettings _instanceSettings;

        #region Ctor
        public TwitterTweetsService(ITwitterAuthenticationInitializer twitterAuthenticationInitializer, ITwitterStatisticsHandler statisticsHandler, ICachedTwitterUserService twitterUserService, ITwitterUserDal twitterUserDal, InstanceSettings instanceSettings, ILogger<TwitterTweetsService> logger)
        {
            _twitterAuthenticationInitializer = twitterAuthenticationInitializer;
            _statisticsHandler = statisticsHandler;
            _twitterUserService = twitterUserService;
            _twitterUserDal = twitterUserDal;
            _instanceSettings = instanceSettings;
            _logger = logger;
        }
        #endregion


        public async Task<ExtractedTweet> GetTweetAsync(long statusId)
        {

            var client = await _twitterAuthenticationInitializer.MakeHttpClient();


            // https://platform.twitter.com/embed/Tweet.html?id=1633788842770825216
            string reqURL =
                "https://api.twitter.com/graphql/XjlydVWHFIDaAUny86oh2g/TweetDetail?variables=%7B%22focalTweetId%22%3A%22"
                + statusId +
                "%22,%22with_rux_injections%22%3Atrue,%22includePromotedContent%22%3Afalse,%22withCommunity%22%3Afalse,%22withQuickPromoteEligibilityTweetFields%22%3Afalse,%22withBirdwatchNotes%22%3Afalse,%22withSuperFollowsUserFields%22%3Afalse,%22withDownvotePerspective%22%3Afalse,%22withReactionsMetadata%22%3Afalse,%22withReactionsPerspective%22%3Afalse,%22withSuperFollowsTweetFields%22%3Afalse,%22withVoice%22%3Atrue,%22withV2Timeline%22%3Atrue%7D&features=%7B%22responsive_web_twitter_blue_verified_badge_is_enabled%22%3Atrue,%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue,%22verified_phone_label_enabled%22%3Afalse,%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue,%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse,%22tweetypie_unmention_optimization_enabled%22%3Atrue,%22vibe_api_enabled%22%3Atrue,%22responsive_web_edit_tweet_api_enabled%22%3Atrue,%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Afalse,%22view_counts_everywhere_api_enabled%22%3Atrue,%22longform_notetweets_consumption_enabled%22%3Atrue,%22tweet_awards_web_tipping_enabled%22%3Afalse,%22freedom_of_speech_not_reach_fetch_enabled%22%3Afalse,%22standardized_nudges_misinfo%22%3Atrue,%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Afalse,%22interactive_text_enabled%22%3Atrue,%22responsive_web_text_conversations_enabled%22%3Afalse,%22longform_notetweets_richtext_consumption_enabled%22%3Afalse,%22responsive_web_enhance_cards_enabled%22%3Atrue%7D";
            using var request = _twitterAuthenticationInitializer.MakeHttpRequest(new HttpMethod("GET"), reqURL, true);
            try
            {
                JsonDocument tweet;
                var httpResponse = await client.SendAsync(request);
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Error retrieving tweet {statusId}; refreshing client", statusId);
                    await _twitterAuthenticationInitializer.RefreshClient(request);
                }
                httpResponse.EnsureSuccessStatusCode();
                var c = await httpResponse.Content.ReadAsStringAsync();
                tweet = JsonDocument.Parse(c);


                var timeline = tweet.RootElement.GetProperty("data").GetProperty("threaded_conversation_with_injections_v2")
                    .GetProperty("instructions").EnumerateArray().First().GetProperty("entries").EnumerateArray();

                var tweetInDoc = timeline.Where(x => x.GetProperty("entryId").GetString() == "tweet-" + statusId)
                    .ToArray().First();
                return await Extract( tweetInDoc );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving tweet {TweetId}", statusId);
                await _twitterAuthenticationInitializer.RefreshClient(request);
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


            var reqURL =
                "https://api.twitter.com/graphql/pNl8WjKAvaegIoVH--FuoQ/UserTweetsAndReplies?variables=%7B%22userId%22%3A%22" +
                userId + "%22,%22count%22%3A40,%22includePromotedContent%22%3Atrue,%22withCommunity%22%3Atrue,%22withSuperFollowsUserFields%22%3Atrue,%22withDownvotePerspective%22%3Afalse,%22withReactionsMetadata%22%3Afalse,%22withReactionsPerspective%22%3Afalse,%22withSuperFollowsTweetFields%22%3Atrue,%22withVoice%22%3Atrue,%22withV2Timeline%22%3Atrue%7D&features=%7B%22responsive_web_twitter_blue_verified_badge_is_enabled%22%3Atrue,%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue,%22verified_phone_label_enabled%22%3Afalse,%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue,%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse,%22tweetypie_unmention_optimization_enabled%22%3Atrue,%22vibe_api_enabled%22%3Atrue,%22responsive_web_edit_tweet_api_enabled%22%3Atrue,%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue,%22view_counts_everywhere_api_enabled%22%3Atrue,%22longform_notetweets_consumption_enabled%22%3Atrue,%22tweet_awards_web_tipping_enabled%22%3Afalse,%22freedom_of_speech_not_reach_fetch_enabled%22%3Afalse,%22standardized_nudges_misinfo%22%3Atrue,%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Afalse,%22interactive_text_enabled%22%3Atrue,%22responsive_web_text_conversations_enabled%22%3Afalse,%22longform_notetweets_richtext_consumption_enabled%22%3Afalse,%22responsive_web_enhance_cards_enabled%22%3Afalse%7D";
            //reqURL =
            //    """https://twitter.com/i/api/graphql/rIIwMe1ObkGh_ByBtTCtRQ/UserTweets?variables={"userId":"44196397","count":20,"includePromotedContent":true,"withQuickPromoteEligibilityTweetFields":true,"withVoice":true,"withV2Timeline":true}&features={"rweb_lists_timeline_redesign_enabled":true,"responsive_web_graphql_exclude_directive_enabled":true,"verified_phone_label_enabled":false,"creator_subscriptions_tweet_preview_api_enabled":true,"responsive_web_graphql_timeline_navigation_enabled":true,"responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,"tweetypie_unmention_optimization_enabled":true,"responsive_web_edit_tweet_api_enabled":true,"graphql_is_translatable_rweb_tweet_is_translatable_enabled":true,"view_counts_everywhere_api_enabled":true,"longform_notetweets_consumption_enabled":true,"responsive_web_twitter_article_tweet_consumption_enabled":false,"tweet_awards_web_tipping_enabled":false,"freedom_of_speech_not_reach_fetch_enabled":true,"standardized_nudges_misinfo":true,"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled":true,"longform_notetweets_rich_text_read_enabled":true,"longform_notetweets_inline_media_enabled":true,"responsive_web_media_download_video_enabled":false,"responsive_web_enhance_cards_enabled":false}&fieldToggles={"withArticleRichContentState":false}""";
            //reqURL = reqURL.Replace("44196397", userId.ToString());
            JsonDocument results;
            List<ExtractedTweet> extractedTweets = new List<ExtractedTweet>();
            using var request = _twitterAuthenticationInitializer.MakeHttpRequest(new HttpMethod("GET"), reqURL, true);
            try
            {

                var httpResponse = await client.SendAsync(request);
                var c = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Error retrieving timeline of {Username}; refreshing client", username);
                    await _twitterAuthenticationInitializer.RefreshClient(request);
                    return null;
                }
                httpResponse.EnsureSuccessStatusCode();
                results = JsonDocument.Parse(c);

                _statisticsHandler.CalledTweetApi();
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
                        _logger.LogError("Tried getting timeline from user " + username + ", but got error: \n" +
                                         e.Message + e.StackTrace + e.Source);

                    }

                }
            }

            return extractedTweets.ToArray();
        }

        private async Task<ExtractedTweet> Extract(JsonElement tweet)
        {

            JsonElement retweet;
            TwitterUser OriginalAuthor;
            TwitterUser author = null;
            JsonElement inReplyToPostIdElement;
            JsonElement inReplyToUserElement;
            string inReplyToUser = null;
            long? inReplyToPostId = null;
            long retweetId = default;

            string userName = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("core").GetProperty("user_results")
                    .GetProperty("result").GetProperty("legacy").GetProperty("screen_name").GetString();

            JsonElement userDoc = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("core")
                    .GetProperty("user_results").GetProperty("result");

            author = _twitterUserService.Extract(userDoc); 
            
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
                bool isNote = tweet.GetProperty("content").GetProperty("itemContent")
                        .GetProperty("tweet_results").GetProperty("result")
                        .TryGetProperty("note_tweet", out var note);
                if (isNote)
                {
                    MessageContent = note.GetProperty("note_tweet_results").GetProperty("result")
                        .GetProperty("text").GetString();
                }
                OriginalAuthor = null;
                
            }
            else 
            {
                MessageContent = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("legacy").GetProperty("full_text").GetString();
                bool isNote = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .TryGetProperty("note_tweet", out var note);
                if (isNote)
                {
                    MessageContent = note.GetProperty("note_tweet_results").GetProperty("result")
                        .GetProperty("text").GetString();
                }
                string OriginalAuthorUsername = tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("core").GetProperty("user_results").GetProperty("result")
                    .GetProperty("legacy").GetProperty("screen_name").GetString();
                OriginalAuthor = await _twitterUserService.GetUserAsync(OriginalAuthorUsername);
                retweetId = Int64.Parse(tweet.GetProperty("content").GetProperty("itemContent")
                    .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("rest_id").GetString());
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
                    string altText = null;
                    if (media.TryGetProperty("video_info", out _))
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

                    if (media.TryGetProperty("ext_alt_text", out JsonElement altNode))
                    {
                        altText = altNode.GetString();
                    }
                    var m = new ExtractedMedia
                    {
                        MediaType = GetMediaType(type, url),
                        Url = url,
                        AltText = altText
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
                Uri test = new Uri(quoteTweetLink);
                string quoteTweetAcct = test.Segments[1].Replace("/", "");
                string quoteTweetId = test.Segments[3];
                
                quoteTweetLink = quoteTweetLink.Replace("https://twitter.com/", $"https://{_instanceSettings.Domain}/@");
                quoteTweetLink = quoteTweetLink.Replace("/status/", "/");

                //MessageContent.Replace($"https://twitter.com/i/web/status/{}", "");
                MessageContent = MessageContent.Replace($"https://twitter.com/{quoteTweetAcct}/status/{quoteTweetId}", "");
                
                MessageContent = MessageContent + "\n\n" + quoteTweetLink;
            }
            
            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweet.GetProperty("entryId").GetString().Replace("tweet-", "")),
                InReplyToStatusId = inReplyToPostId,
                InReplyToAccount = inReplyToUser,
                MessageContent = MessageContent.Trim(),
                CreatedAt = DateTime.ParseExact(creationTime, "ddd MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture),
                IsReply = isReply,
                IsThread = userName == inReplyToUser,
                IsRetweet = isRetweet,
                Media = Media.Count() == 0 ? null : Media.ToArray(),
                RetweetUrl = "https://t.co/123",
                RetweetId = retweetId,
                OriginalAuthor = OriginalAuthor,
                Author = author,
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
