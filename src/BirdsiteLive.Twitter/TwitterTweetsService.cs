using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Statistics.Domain;
using BirdsiteLive.Twitter.Models;
using BirdsiteLive.Twitter.Tools;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using AngleSharp;
using AngleSharp.Dom;

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
        private readonly IBrowsingContext _context;

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
            
            var config = Configuration.Default.WithDefaultLoader();
            _context = BrowsingContext.New(config);
        }
        #endregion


        public async Task<ExtractedTweet> GetTweetAsync(long statusId)
        {
            //return await TweetFromSyndication(statusId);

            var client = await _twitterAuthenticationInitializer.MakeHttpClient();


            string reqURL =
                "https://twitter.com/i/api/graphql/0hWvDhmW8YQ-S_ib3azIrw/TweetResultByRestId?variables=%7B%22tweetId%22%3A%221519480761749016577%22%2C%22withCommunity%22%3Afalse%2C%22includePromotedContent%22%3Afalse%2C%22withVoice%22%3Afalse%7D&features=%7B%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Atrue%2C%22responsive_web_twitter_article_tweet_consumption_enabled%22%3Afalse%2C%22tweet_awards_web_tipping_enabled%22%3Afalse%2C%22freedom_of_speech_not_reach_fetch_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Atrue%2C%22longform_notetweets_rich_text_read_enabled%22%3Atrue%2C%22longform_notetweets_inline_media_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22responsive_web_media_download_video_enabled%22%3Afalse%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D";
            reqURL = reqURL.Replace("1519480761749016577", statusId.ToString());
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


                var tweetInDoc = tweet.RootElement.GetProperty("data").GetProperty("tweetResult")
                    .GetProperty("result");

                
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
            await Task.delay(1000);
        
            return await TweetFromNitter(user, fromTweetId);

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


            //reqURL =
            //    """https://twitter.com/i/api/graphql/rIIwMe1ObkGh_ByBtTCtRQ/UserTweets?variables={"userId":"44196397","count":20,"includePromotedContent":true,"withQuickPromoteEligibilityTweetFields":true,"withVoice":true,"withV2Timeline":true}&features={"rweb_lists_timeline_redesign_enabled":true,"responsive_web_graphql_exclude_directive_enabled":true,"verified_phone_label_enabled":false,"creator_subscriptions_tweet_preview_api_enabled":true,"responsive_web_graphql_timeline_navigation_enabled":true,"responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,"tweetypie_unmention_optimization_enabled":true,"responsive_web_edit_tweet_api_enabled":true,"graphql_is_translatable_rweb_tweet_is_translatable_enabled":true,"view_counts_everywhere_api_enabled":true,"longform_notetweets_consumption_enabled":true,"responsive_web_twitter_article_tweet_consumption_enabled":false,"tweet_awards_web_tipping_enabled":false,"freedom_of_speech_not_reach_fetch_enabled":true,"standardized_nudges_misinfo":true,"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled":true,"longform_notetweets_rich_text_read_enabled":true,"longform_notetweets_inline_media_enabled":true,"responsive_web_media_download_video_enabled":false,"responsive_web_enhance_cards_enabled":false}""";
            //reqURL = reqURL.Replace("44196397", userId.ToString());
            string reqURL =
                """https://twitter.com/i/api/graphql/XicnWRbyQ3WgVY__VataBQ/UserTweets?variables={"userId":""" + '"' + userId + '"' + ""","count":20,"includePromotedContent":true,"withQuickPromoteEligibilityTweetFields":true,"withVoice":true,"withV2Timeline":true}&features={"rweb_lists_timeline_redesign_enabled":true,"responsive_web_graphql_exclude_directive_enabled":true,"verified_phone_label_enabled":false,"creator_subscriptions_tweet_preview_api_enabled":true,"responsive_web_graphql_timeline_navigation_enabled":true,"responsive_web_graphql_skip_user_profile_image_extensions_enabled":false,"tweetypie_unmention_optimization_enabled":true,"responsive_web_edit_tweet_api_enabled":true,"graphql_is_translatable_rweb_tweet_is_translatable_enabled":true,"view_counts_everywhere_api_enabled":true,"longform_notetweets_consumption_enabled":true,"responsive_web_twitter_article_tweet_consumption_enabled":false,"tweet_awards_web_tipping_enabled":false,"freedom_of_speech_not_reach_fetch_enabled":true,"standardized_nudges_misinfo":true,"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled":true,"longform_notetweets_rich_text_read_enabled":true,"longform_notetweets_inline_media_enabled":true,"responsive_web_media_download_video_enabled":false,"responsive_web_enhance_cards_enabled":false}""";
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
                        JsonElement tweetRes = tweet.GetProperty("content").GetProperty("itemContent")
                            .GetProperty("tweet_results").GetProperty("result");
                        var extractedTweet = await Extract(tweetRes);

                        extractedTweets.Add(extractedTweet);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Tried getting timeline from user " + username + ", but got error: \n" +
                                         e.Message + e.StackTrace + e.Source);

                    }

                }
            }
            extractedTweets = extractedTweets.OrderByDescending(x => x.Id).Where(x => x.Id > fromTweetId).ToList();
            _statisticsHandler.GotNewTweets(extractedTweets.Count);

            return extractedTweets.ToArray();
        }

        private async Task<ExtractedTweet[]> TweetFromNitter(SyncTwitterUser user, long fromId)
        {
            List<string> domains = new List<string>() {"nitter.poast.org", "nitter.privacydev.net", "nitter.d420.de", "nitter.nicfab.eu", "nitter.salastil.com"} ;
            Random rnd = new Random();
            int randIndex = rnd.Next(domains.Count);
            var domain = domains[randIndex];
            //domain = domains.Last();
            var address = $"https://{domain}/{user.Acct}/with_replies";
            var document = await _context.OpenAsync(address);
            _statisticsHandler.CalledApi("Nitter");
                
            var cellSelector = ".tweet-link";
            var cells = document.QuerySelectorAll(cellSelector);
            var titles = cells.Select(m => m.GetAttribute("href"));

            List<ExtractedTweet> tweets = new List<ExtractedTweet>();
            string pattern = @".*\/([0-9]+)#m";
            Regex rg = new Regex(pattern);
            foreach (string title in titles)
            {
                MatchCollection matchedId = rg.Matches(title);
                var matchString = matchedId[0].Groups[1].Value;
                var match = Int64.Parse(matchString);

                if (match < fromId)
                    break;
                
                var tweet = await TweetFromSyndication(match);
                if (tweet.Author.Acct != user.Acct)
                {
                    tweet.IsRetweet = true;
                    tweet.OriginalAuthor = tweet.Author;
                    tweet.Author = await _twitterUserService.GetUserAsync(user.Acct);
                    tweet.RetweetId = tweet.Id;
                    // Sadly not given by Nitter UI
                    tweet.Id = new Random().NextInt64(1000002530833240064, 1266812530833240064);
                }
                tweets.Add(tweet);
                await Task.Delay(100);
            }
            
            _statisticsHandler.GotNewTweets(tweets.Count);
            return tweets.ToArray();
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

                messageContent = Regex.Replace(messageContent, Regex.Escape($"https://twitter.com/{quoteTweetAcct}/status/{quoteTweetId}"), "", RegexOptions.IgnoreCase);
                messageContent = messageContent + "\n\n" + quoteTweetLink;
                
            }

            var author = new TwitterUser()
            {
                Acct = username,
            };

            var createdaAt = DateTime.Parse(tweet.RootElement.GetProperty("created_at").GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind);
            
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
                CreatedAt = createdaAt,
                Media = Media.Count() == 0 ? null : Media.ToArray(),
            };

        }

        private async Task<ExtractedTweet> Extract(JsonElement tweetRes)
        {

            JsonElement retweet;
            TwitterUser OriginalAuthor;
            TwitterUser author = null;
            JsonElement inReplyToPostIdElement;
            JsonElement inReplyToUserElement;
            string inReplyToUser = null;
            long? inReplyToPostId = null;
            long retweetId = default;

            //JsonElement tweetRes__ = tweet.GetProperty("content").GetProperty("itemContent")
            //    .GetProperty("tweet_results").GetProperty("result");
            JsonElement userDoc = tweetRes.GetProperty("core")
                    .GetProperty("user_results").GetProperty("result");

            string userName = userDoc.GetProperty("legacy").GetProperty("screen_name").GetString();


            author = _twitterUserService.Extract(userDoc); 
            
            bool isReply = tweetRes.GetProperty("legacy")
                    .TryGetProperty("in_reply_to_status_id_str", out inReplyToPostIdElement);
            tweetRes.GetProperty("legacy")
                    .TryGetProperty("in_reply_to_screen_name", out inReplyToUserElement);
            if (isReply) 
            {
                inReplyToPostId = Int64.Parse(inReplyToPostIdElement.GetString());
                inReplyToUser = inReplyToUserElement.GetString();
            }
            bool isRetweet = tweetRes.GetProperty("legacy")
                    .TryGetProperty("retweeted_status_result", out retweet);
            string MessageContent;
            if (!isRetweet)
            {
                MessageContent = tweetRes.GetProperty("legacy")
                    .GetProperty("full_text").GetString();
                bool isNote = tweetRes.TryGetProperty("note_tweet", out var note);
                if (isNote)
                {
                    MessageContent = note.GetProperty("note_tweet_results").GetProperty("result")
                        .GetProperty("text").GetString();
                }
                OriginalAuthor = null;
                
            }
            else 
            {
                MessageContent = tweetRes.GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("legacy").GetProperty("full_text").GetString();
                bool isNote = tweetRes.GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .TryGetProperty("note_tweet", out var note);
                if (isNote)
                {
                    MessageContent = note.GetProperty("note_tweet_results").GetProperty("result")
                        .GetProperty("text").GetString();
                }
                JsonElement OriginalAuthorDoc = tweetRes.GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("core").GetProperty("user_result").GetProperty("result");
                OriginalAuthor = _twitterUserService.Extract(OriginalAuthorDoc); 
                //OriginalAuthor = await _twitterUserService.GetUserAsync(OriginalAuthorUsername);
                retweetId = Int64.Parse(tweetRes.GetProperty("legacy")
                    .GetProperty("retweeted_status_result").GetProperty("result")
                    .GetProperty("rest_id").GetString());
            }

            string creationTime = tweetRes.GetProperty("legacy")
                    .GetProperty("created_at").GetString().Replace(" +0000", "");

            JsonElement extendedEntities;
            bool hasMedia = tweetRes.GetProperty("legacy")
                    .TryGetProperty("extended_entities", out extendedEntities);

            JsonElement.ArrayEnumerator urls = tweetRes.GetProperty("legacy")
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

            bool isQuoteTweet = tweetRes.GetProperty("legacy")
                    .GetProperty("is_quote_status").GetBoolean();

            if (isQuoteTweet) 
            {

                string quoteTweetId = tweetRes.GetProperty("legacy")
                        .GetProperty("quoted_status_id_str").GetString();
                JsonElement quoteTweetAcctDoc = tweetRes
                    .GetProperty("quoted_status_result").GetProperty("result")
                    .GetProperty("core").GetProperty("user_results").GetProperty("result");
                TwitterUser QTauthor = _twitterUserService.Extract(quoteTweetAcctDoc);
                string quoteTweetAcct = QTauthor.Acct;
                //Uri test = new Uri(quoteTweetLink);
                //string quoteTweetAcct = test.Segments[1].Replace("/", "");
                //string quoteTweetId = test.Segments[3];
                
                string quoteTweetLink = $"https://{_instanceSettings.Domain}/@{quoteTweetAcct}/{quoteTweetId}";

                //MessageContent.Replace($"https://twitter.com/i/web/status/{}", "");
               // MessageContent = MessageContent.Replace($"https://twitter.com/{quoteTweetAcct}/status/{quoteTweetId}", "");
                MessageContent = MessageContent.Replace($"https://twitter.com/{quoteTweetAcct}/status/{quoteTweetId}", "", StringComparison.OrdinalIgnoreCase);
                
                //MessageContent = Regex.Replace(MessageContent, Regex.Escape($"https://twitter.com/{quoteTweetAcct}/status/{quoteTweetId}"), "", RegexOptions.IgnoreCase);
                MessageContent = MessageContent + "\n\n" + quoteTweetLink;
            }
            
            var extractedTweet = new ExtractedTweet
            {
                Id = Int64.Parse(tweetRes.GetProperty("rest_id").GetString()),
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
