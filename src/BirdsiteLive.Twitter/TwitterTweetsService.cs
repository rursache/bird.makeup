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
        Task<ExtractedTweet[]> GetTimelineAsync(SyncTwitterUser user, long fromTweetId = -1);
    }

    public class TwitterTweetsService : ITwitterTweetsService
    {
        private readonly ITwitterAuthenticationInitializer _twitterAuthenticationInitializer;
        private readonly ITwitterStatisticsHandler _statisticsHandler;
        private readonly ICachedTwitterUserService _twitterUserService;
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly ILogger<TwitterTweetsService> _logger;
        private readonly InstanceSettings _instanceSettings;
        private readonly IHttpClientFactory _httpClientFactory;
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
        public TwitterTweetsService(ITwitterAuthenticationInitializer twitterAuthenticationInitializer, ITwitterStatisticsHandler statisticsHandler, ICachedTwitterUserService twitterUserService, ITwitterUserDal twitterUserDal, InstanceSettings instanceSettings, IHttpClientFactory httpClientFactory, ILogger<TwitterTweetsService> logger)
        {
            _twitterAuthenticationInitializer = twitterAuthenticationInitializer;
            _statisticsHandler = statisticsHandler;
            _twitterUserService = twitterUserService;
            _twitterUserDal = twitterUserDal;
            _instanceSettings = instanceSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        #endregion


        public async Task<ExtractedTweet> GetTweetAsync(long statusId)
        {
            return await TweetFromSyndication(statusId);

            var client = await _twitterAuthenticationInitializer.MakeHttpClient();


            // https://platform.twitter.com/embed/Tweet.html?id=1633788842770825216
            string reqURL =
                "https://twitter.com/i/api/graphql/XicnWRbyQ3WgVY__VataBQ/UserTweets?variables={%22focalTweetId%22%3A%22"
                + statusId +
                "%22,%22count%22:20,%22includeHasBirdwatchNotes%22:false}&features="+ gqlFeatures;
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


                var timeline = tweet.RootElement.GetProperty("data").GetProperty("timeline_response")
                    .GetProperty("instructions").EnumerateArray().First().GetProperty("entries").EnumerateArray();

                var tweetInDoc = timeline.Where(x => x.GetProperty("entryId").GetString() == "tweet-" + statusId)
                    .ToArray().First();
                
                _statisticsHandler.GotNewTweets(1);
                
                return await Extract( tweetInDoc );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving tweet {TweetId}", statusId);
                await _twitterAuthenticationInitializer.RefreshClient(request);
                return null;
            }
        }

        public async Task<ExtractedTweet[]> GetTimelineAsync(SyncTwitterUser user, long fromTweetId = -1)
        {

            return Array.Empty<ExtractedTweet>();
            var client = await _twitterAuthenticationInitializer.MakeHttpClient();

            long userId;
            string username = user.Acct;
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
                "https://twitter.com/i/api/graphql/XicnWRbyQ3WgVY__VataBQ/UserTweets?variables=%7B%22userId%22%3A%22" +
                userId +
                "%22,%22count%22%3A40,%22includeHasBirdwatchNotes%22%3Atrue}&features=" +
                gqlFeatures;
            //reqURL =
            //    """https://twitter.com/i/api/graphql/rIIwMe1ObkGh_ByBtTCtRQ/UserTweets?variables={"userId":"44196397","count":20,"includePromotedContent":true,"withQuickPromoteEligibilityTweetFields":true,"withVoice":true,"withV2Timeline":true}&features={"rweb_lists_timeline_redesign_enabled":true,"responsive_web_graphql_exclude_directive_enabled":true,"verified_phone_label_enabled":false,"creator_subscriptions_tweet_preview_api_enabled":true,"responsive_web_graphql_timeline_navigation_enabled":true,"responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,"tweetypie_unmention_optimization_enabled":true,"responsive_web_edit_tweet_api_enabled":true,"graphql_is_translatable_rweb_tweet_is_translatable_enabled":true,"view_counts_everywhere_api_enabled":true,"longform_notetweets_consumption_enabled":true,"responsive_web_twitter_article_tweet_consumption_enabled":false,"tweet_awards_web_tipping_enabled":false,"freedom_of_speech_not_reach_fetch_enabled":true,"standardized_nudges_misinfo":true,"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled":true,"longform_notetweets_rich_text_read_enabled":true,"longform_notetweets_inline_media_enabled":true,"responsive_web_media_download_video_enabled":false,"responsive_web_enhance_cards_enabled":false}""";
            //reqURL = reqURL.Replace("44196397", userId.ToString());
            //reqURL =
            //    """https://twitter.com/i/api/graphql/XicnWRbyQ3WgVY__VataBQ/UserTweets?variables={"userId":"44196397","count":20,"includePromotedContent":true,"withQuickPromoteEligibilityTweetFields":true,"withVoice":true,"withV2Timeline":true}&features={"rweb_lists_timeline_redesign_enabled":true,"responsive_web_graphql_exclude_directive_enabled":true,"verified_phone_label_enabled":false,"creator_subscriptions_tweet_preview_api_enabled":true,"responsive_web_graphql_timeline_navigation_enabled":true,"responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,"tweetypie_unmention_optimization_enabled":true,"responsive_web_edit_tweet_api_enabled":true,"graphql_is_translatable_rweb_tweet_is_translatable_enabled":true,"view_counts_everywhere_api_enabled":true,"longform_notetweets_consumption_enabled":true,"responsive_web_twitter_article_tweet_consumption_enabled":false,"tweet_awards_web_tipping_enabled":false,"freedom_of_speech_not_reach_fetch_enabled":true,"standardized_nudges_misinfo":true,"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled":true,"longform_notetweets_rich_text_read_enabled":true,"longform_notetweets_inline_media_enabled":true,"responsive_web_media_download_video_enabled":false,"responsive_web_enhance_cards_enabled":false}""";
            JsonDocument results;
            List<ExtractedTweet> extractedTweets = new List<ExtractedTweet>();
            using var request = _twitterAuthenticationInitializer.MakeHttpRequest(new HttpMethod("GET"), reqURL, true);
            try
            {

                var httpResponse = await client.SendAsync(request);
                var c = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
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
                    if (tweet.GetProperty("content").GetProperty("__typename").GetString() != "TimelineTimelineItem")
                        continue;
                    

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
            _statisticsHandler.GotNewTweets(extractedTweets.Count);

            return extractedTweets.ToArray();
        }

        private async Task<ExtractedTweet> TweetFromSyndication(long statusId)
        {
            string reqURL =
                $"https://cdn.syndication.twimg.com/tweet-result?id={statusId}&lang=en&token=3ykp5xr72qv";
            JsonDocument tweet;
            var client = _httpClientFactory.CreateClient();
            
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://cdn.syndication.twimg.com/tweet-result?id={statusId}&lang=en&token=3ykp5xr72qv"),
            };
            //request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36");
            request.Headers.Add("User-Agent", "farts");
            //using var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL);
            //using var request = _twitterAuthenticationInitializer.MakeHttpRequest(HttpMethod.Get, reqURL, false);
            
            using var httpResponse = await client.SendAsync(request);
            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Error retrieving tweet {statusId}; refreshing client", statusId);
            }

            httpResponse.EnsureSuccessStatusCode();
            var c = await httpResponse.Content.ReadAsStringAsync();
            tweet = JsonDocument.Parse(c);

            
            string messageContent = tweet.RootElement.GetProperty("text").GetString();
            string username = tweet.RootElement.GetProperty("user").GetProperty("screen_name").GetString().ToLower();
            List<ExtractedMedia> Media = new();

            JsonElement replyTo;
            bool isReply = tweet.RootElement.TryGetProperty("parent", out replyTo);
            string inReplyTo = null;
            long? inReplyToId = null;
            bool isThread = false;
            if (isReply)
            {
                inReplyTo = tweet.RootElement.GetProperty("in_reply_to_screen_name").GetString();
                inReplyToId = Int64.Parse(tweet.RootElement.GetProperty("in_reply_to_status_id_str").GetString());

                isThread = username == inReplyTo;
            }

            JsonElement entities;
            if (tweet.RootElement.TryGetProperty("entities", out entities))
            {
                JsonElement urls;
                if (entities.TryGetProperty("urls", out urls))
                {
                    foreach (JsonElement url in urls.EnumerateArray())
                    {
                        var urlTCO = url.GetProperty("url").GetString();
                        var urlOriginal = url.GetProperty("expanded_url").GetString();

                        messageContent = messageContent.Replace(urlTCO, urlOriginal);
                    }
                }
                
                JsonElement mediaEntity;
                if (entities.TryGetProperty("media", out mediaEntity))
                {
                    foreach (JsonElement media in mediaEntity.EnumerateArray())
                    {
                        var urlTCO = media.GetProperty("url").GetString();

                        messageContent = messageContent.Replace(urlTCO, String.Empty);
                    }
                }
            }
            
            JsonElement mediaDetails;
            if (tweet.RootElement.TryGetProperty("mediaDetails", out mediaDetails))
            {
                foreach (var media in mediaDetails.EnumerateArray())
                {
                        var url = media.GetProperty("media_url_https").GetString();
                        var type = media.GetProperty("type").GetString();
                        string altText = null;
                        if (media.TryGetProperty("ext_alt_text", out _))
                            altText = media.GetProperty("ext_alt_text").GetString();
                        string returnType = null;

                        if (type == "photo")
                        {
                            returnType = "image/jpeg";
                        }
                        else if (type == "video")
                        {
                            returnType = "video/mp4";
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
                        
                        var m = new ExtractedMedia()
                        {
                            Url = url,
                            MediaType = returnType,
                            AltText = altText,
                        };
                        Media.Add(m);
                }
            }

            JsonElement qt;
            bool isQT = tweet.RootElement.TryGetProperty("quoted_tweet", out qt);
            if (isQT)
            {
                string quoteTweetId = qt.GetProperty("id_str").GetString();
                string quoteTweetAcct = qt.GetProperty("user").GetProperty("screen_name").GetString();
                
                string quoteTweetLink = $"https://{_instanceSettings.Domain}/@{quoteTweetAcct}/{quoteTweetId}";

                messageContent = messageContent.Replace($"https://twitter.com/{quoteTweetAcct}/status/{quoteTweetId}", "");
                messageContent = messageContent + "\n\n" + quoteTweetLink;
                
            }

            var author = new TwitterUser()
            {
                Acct = username,
            };
            
            return new ExtractedTweet()
            {
                MessageContent = messageContent.Trim(),
                Id = statusId,
                IsReply = isReply,
                IsThread = isThread,
                IsRetweet = false,
                InReplyToAccount = inReplyTo,
                InReplyToStatusId = inReplyToId,
                Author = author,
                Media = Media.Count() == 0 ? null : Media.ToArray(),
            };

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

            string userName = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("core").GetProperty("user_result")
                    .GetProperty("result").GetProperty("legacy").GetProperty("screen_name").GetString();

            JsonElement userDoc = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("core")
                    .GetProperty("user_result").GetProperty("result");

            author = _twitterUserService.Extract(userDoc); 
            
            bool isReply = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("in_reply_to_status_id_str", out inReplyToPostIdElement);
            tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("in_reply_to_screen_name", out inReplyToUserElement);
            if (isReply) 
            {
                inReplyToPostId = Int64.Parse(inReplyToPostIdElement.GetString());
                inReplyToUser = inReplyToUserElement.GetString();
            }
            bool isRetweet = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("retweeted_status_result", out retweet);
            string MessageContent;
            if (!isRetweet)
            {
                MessageContent = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("full_text").GetString();
                bool isNote = tweet.GetProperty("content").GetProperty("content")
                        .GetProperty("tweetResult").GetProperty("result")
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
                MessageContent = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("legacy").GetProperty("full_text").GetString();
                bool isNote = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .TryGetProperty("note_tweet", out var note);
                if (isNote)
                {
                    MessageContent = note.GetProperty("note_tweet_results").GetProperty("result")
                        .GetProperty("text").GetString();
                }
                string OriginalAuthorUsername = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("core").GetProperty("user_result").GetProperty("result")
                    .GetProperty("legacy").GetProperty("screen_name").GetString();
                JsonElement OriginalAuthorDoc = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("core").GetProperty("user_result").GetProperty("result");
                OriginalAuthor = _twitterUserService.Extract(OriginalAuthorDoc); 
                //OriginalAuthor = await _twitterUserService.GetUserAsync(OriginalAuthorUsername);
                retweetId = Int64.Parse(tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("rest_id").GetString());
            }

            string creationTime = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("created_at").GetString().Replace(" +0000", "");

            JsonElement extendedEntities;
            bool hasMedia = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .TryGetProperty("extended_entities", out extendedEntities);

            JsonElement.ArrayEnumerator urls = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
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

            bool isQuoteTweet = tweet.GetProperty("content").GetProperty("content")
                    .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                    .GetProperty("is_quote_status").GetBoolean();

            if (isQuoteTweet) 
            {

                string quoteTweetId = tweet.GetProperty("content").GetProperty("content")
                        .GetProperty("tweetResult").GetProperty("result").GetProperty("legacy")
                        .GetProperty("quoted_status_id_str").GetString();
                string quoteTweetAcct = tweet.GetProperty("content").GetProperty("content")
                        .GetProperty("tweetResult").GetProperty("result")
                        .GetProperty("quoted_status_result").GetProperty("result")
                        .GetProperty("core").GetProperty("user_result").GetProperty("result")
                        .GetProperty("legacy").GetProperty("screen_name").GetString();
                //Uri test = new Uri(quoteTweetLink);
                //string quoteTweetAcct = test.Segments[1].Replace("/", "");
                //string quoteTweetId = test.Segments[3];
                
                string quoteTweetLink = $"https://{_instanceSettings.Domain}/@{quoteTweetAcct}/{quoteTweetId}";

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
