using System;
using System.Collections.Generic;
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
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.twitter.com/2/tweets?ids=" + statusId + "&tweet.fields=id,created_at,text,author_id,in_reply_to_user_id,referenced_tweets,attachments,withheld,geo,entities,public_metrics,possibly_sensitive,source,lang,context_annotations,conversation_id,reply_settings&user.fields=name,username"))
    {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _twitterAuthenticationInitializer.Token); 

                    var httpResponse = await _httpClient.SendAsync(request);
                    httpResponse.EnsureSuccessStatusCode();
                    var c = await httpResponse.Content.ReadAsStringAsync();
                    tweet = JsonDocument.Parse(c);
                }

                _statisticsHandler.CalledTweetApi();
                if (tweet == null) return null; //TODO: test this
                return tweet.RootElement.GetProperty("data").EnumerateArray().Select<JsonElement, ExtractedTweet>(Extract).ToArray().First();
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

            return tweets.RootElement.GetProperty("data").EnumerateArray().Select<JsonElement, ExtractedTweet>(Extract).ToArray();
        }

        public ExtractedTweet Extract(JsonElement tweet)
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
                    var statusId = Int64.Parse(first.GetProperty("id").GetString());
                    var extracted = GetTweet(statusId);
                    extracted.IsRetweet = true;
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

            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweet.GetProperty("id").GetString()),
                InReplyToStatusId = replyId,
                InReplyToAccount = replyAccountString,
                MessageContent = tweet.GetProperty("text").GetString(),
                Media = Array.Empty<ExtractedMedia>(),
                CreatedAt = DateTime.Now, // tweet.GetProperty("data").GetProperty("in_reply_to_status_id").GetDateTime(),
                IsReply = IsReply,
                IsThread = false,
                IsRetweet = IsRetweet,
                RetweetUrl = "https://t.co/123"
            };

            return extractedTweet;
        }
    }
}
