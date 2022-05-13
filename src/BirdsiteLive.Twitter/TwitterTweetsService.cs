using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text.Json;
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
                await _twitterAuthenticationInitializer.EnsureAuthenticationIsInitialized();
                JsonDocument tweet;
                var reqURL = "https://api.twitter.com/2/tweets/"  + statusId
                     + "?expansions=author_id,referenced_tweets.id,attachments.media_keys,entities.mentions.username,referenced_tweets.id.author_id&tweet.fields=id,created_at,text,author_id,in_reply_to_user_id,referenced_tweets,attachments,withheld,geo,entities,public_metrics,possibly_sensitive,source,lang,context_annotations,conversation_id,reply_settings&user.fields=id,created_at,name,username,protected,verified,withheld,profile_image_url,location,url,description,entities,pinned_tweet_id,public_metrics&media.fields=media_key,duration_ms,height,preview_image_url,type,url,width,public_metrics,alt_text,variants";
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL))
    {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _twitterAuthenticationInitializer.Token); 

                    var httpResponse = await _httpClient.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();
                    var c = await httpResponse.Content.ReadAsStringAsync();
                    tweet = JsonDocument.Parse(c);
                }

                _statisticsHandler.CalledTweetApi();
                if (tweet == null) return null; //TODO: test this

                JsonElement mediaExpension;
                tweet.RootElement.GetProperty("includes").TryGetProperty("media", out mediaExpension);

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

            await _twitterAuthenticationInitializer.EnsureAuthenticationIsInitialized();

            var user = _twitterUserService.GetUser(username);
            if (user == null || user.Protected) return new ExtractedTweet[0];

            var reqURL = "https://api.twitter.com/2/users/" 
                 + user.Id + 
                 "/tweets?expansions=in_reply_to_user_id,attachments.media_keys,entities.mentions.username,referenced_tweets.id.author_id&tweet.fields=id"
                 + "&media.fields=media_key,duration_ms,height,preview_image_url,type,url,width,public_metrics,alt_text,variants"
                 + "&max_results=5"
                 + "" ; // ?since_id=2324234234
            JsonDocument tweets;
            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL))
    {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _twitterAuthenticationInitializer.Token); 

                    var httpResponse = await _httpClient.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();
                    var c = await httpResponse.Content.ReadAsStringAsync();
                    tweets = JsonDocument.Parse(c);
                }

                _statisticsHandler.CalledTweetApi();
                if (tweets == null) return null; //TODO: test this
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving timeline ", username);
                return null;
            }

            JsonElement mediaExpension;
            tweets.RootElement.TryGetProperty("media", out mediaExpension);

            return tweets.RootElement.GetProperty("data").EnumerateArray().Select<JsonElement, ExtractedTweet>(x => Extract(x, mediaExpension)).ToArray();
        }

        private ExtractedTweet Extract(JsonElement tweet, JsonElement media)
        {
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
                    extracted.Id = Int64.Parse(tweet.GetProperty("id").GetString());
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
            if (tweet.TryGetProperty("attachments", out attachments))
            {
                foreach (JsonElement m in attachments.GetProperty("media_keys").EnumerateArray())
                {
                    var mediaInfo = media.EnumerateArray().Where(x => x.GetProperty("media_key").GetString() == m.GetString()).First();
                    var url = mediaInfo.GetProperty("url").GetString();
                    var mediaType = mediaInfo.GetProperty("type").GetString();
                    extractedMedia.Append(
                        new ExtractedMedia 
                        {
                            Url = url,
                            MediaType = GetMediaType(mediaType, url),
                        }
                    );

                }
            }


            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweet.GetProperty("id").GetString()),
                InReplyToStatusId = replyId,
                InReplyToAccount = replyAccountString,
                MessageContent = tweet.GetProperty("text").GetString(),
                CreatedAt = DateTime.Now, // tweet.GetProperty("data").GetProperty("in_reply_to_status_id").GetDateTime(),
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
