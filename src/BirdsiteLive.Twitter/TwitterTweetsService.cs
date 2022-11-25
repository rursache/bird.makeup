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
            try
            {
                var client = await _twitterAuthenticationInitializer.MakeHttpClient();
                JsonDocument tweet;
                var reqURL = "https://api.twitter.com/2/tweets/"  + statusId
                     + "?expansions=author_id,referenced_tweets.id,attachments.media_keys,entities.mentions.username,referenced_tweets.id.author_id&tweet.fields=id,created_at,text,author_id,in_reply_to_user_id,referenced_tweets,attachments,withheld,geo,entities,public_metrics,possibly_sensitive,source,lang,context_annotations,conversation_id,reply_settings&user.fields=id,created_at,name,username,protected,verified,withheld,profile_image_url,location,url,description,entities,pinned_tweet_id,public_metrics&media.fields=media_key,duration_ms,height,preview_image_url,type,url,width,public_metrics,alt_text,variants";
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL))
                {
                    var httpResponse = await client.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();
                    var c = await httpResponse.Content.ReadAsStringAsync();
                    tweet = JsonDocument.Parse(c);
                }

                _statisticsHandler.CalledTweetApi();
                if (tweet == null) return null; //TODO: test this

                JsonElement mediaExpension = default;
                try 
                {
                    tweet.RootElement.GetProperty("includes").TryGetProperty("media", out mediaExpension);
                } 
                catch (Exception)
                { }

                //return tweet.RootElement.GetProperty("data").EnumerateArray().Select<JsonElement, ExtractedTweet>(x => Extract(x, mediaExpension)).ToArray().First();
                return Extract( tweet.RootElement.GetProperty("data"), mediaExpension);
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


            var reqURL = "https://twitter.com/i/api/graphql/25oeBocoJ0NLTbSBegxleg/UserTweets?variables=%7B%22userId%22%3A%22"
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
                    try 
                    {

                        var extractedTweet = new ExtractedTweet
                        {
                            Id = Int64.Parse(tweet.GetProperty("sortIndex").GetString()),
                            InReplyToStatusId = null,
                            InReplyToAccount = null,
                            MessageContent = tweet.GetProperty("content").GetProperty("itemContent")
                                .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
                                .GetProperty("full_text").GetString(),
                            CreatedAt = DateTime.Now, //tweet.GetProperty("content").GetProperty("itemContent")
//                                .GetProperty("tweet_results").GetProperty("result").GetProperty("legacy")
//                                .GetProperty("created_at").GetDateTime(),
                            IsReply = false,
                            IsThread = false,
                            IsRetweet = false,
                            Media = null,
                            RetweetUrl = "https://t.co/123",
                            OriginalAuthor = null,
                        };
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

        private ExtractedTweet Extract(JsonElement tweet, JsonElement media)
        {
            var id = Int64.Parse(tweet.GetProperty("id").GetString());
            bool IsRetweet = false;
            bool IsReply = false;
            long? replyId = null;
            JsonElement replyAccount;
            string? replyAccountString = null;
            JsonElement referenced_tweets;
            if(tweet.TryGetProperty("in_reply_to_user_id", out replyAccount))
            {
                replyAccountString = replyAccount.GetString();

            }
            if(tweet.TryGetProperty("referenced_tweets", out referenced_tweets))
            {
                var first = referenced_tweets.EnumerateArray().ToList()[0];
                if (first.GetProperty("type").GetString() == "retweeted")
                {
                    IsRetweet = true;
                    var regex = new Regex("RT @([A-Za-z0-9_]+):");
                    var match = regex.Match(tweet.GetProperty("text").GetString());
                    var originalAuthor = _twitterUserService.GetUser(match.Groups[1].Value);
                    var statusId = Int64.Parse(first.GetProperty("id").GetString());
                    var extracted = GetTweet(statusId);
                    extracted.RetweetId = id;
                    extracted.IsRetweet = true;
                    extracted.OriginalAuthor = originalAuthor;
                    return extracted;

                }
                if (first.GetProperty("type").GetString() == "replied_to")
                {
                    IsReply = true;
                    replyId = Int64.Parse(first.GetProperty("id").GetString());
                }
                if (first.GetProperty("type").GetString() == "quoted")
                {
                    IsReply = true;
                    replyId = Int64.Parse(first.GetProperty("id").GetString());
                }
            }

            var extractedMedia = Array.Empty<ExtractedMedia>();
            JsonElement attachments;
            try 
            {
                if (tweet.TryGetProperty("attachments", out attachments))
                {
                    foreach (JsonElement m in attachments.GetProperty("media_keys").EnumerateArray())
                    {
                        var mediaInfo = media.EnumerateArray().Where(x => x.GetProperty("media_key").GetString() == m.GetString()).First();
                        var mediaType = mediaInfo.GetProperty("type").GetString();
                        if (mediaType != "photo")
                        {
                            continue;
                        }
                        var url = mediaInfo.GetProperty("url").GetString();
                        extractedMedia.Append(
                            new ExtractedMedia 
                            {
                                Url = url,
                                MediaType = GetMediaType(mediaType, url),
                            }
                        );

                    }
                }

            }
            catch (Exception e)
            {
                _logger.LogError("Tried getting media from tweet " + id + ", but got error: \n" + e.Message + e.StackTrace + e.Source);

            }


            var extractedTweet = new ExtractedTweet
            {
                Id = id,
                InReplyToStatusId = replyId,
                InReplyToAccount = replyAccountString,
                MessageContent = tweet.GetProperty("text").GetString(),
                CreatedAt = tweet.GetProperty("created_at").GetDateTime(),
                IsReply = IsReply,
                IsThread = false,
                IsRetweet = IsRetweet,
                Media = extractedMedia,
                RetweetUrl = "https://t.co/123",
                OriginalAuthor = null,
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
