using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BirdsiteLive.Twitter.Models;

namespace BirdsiteLive.Twitter.Extractors
{
    public interface ITweetExtractor
    {
        ExtractedTweet Extract(JsonElement tweet);
    }

    public class TweetExtractor : ITweetExtractor
    {   

        private readonly ITwitterTweetsService _twitterTweetsService;

        public TweetExtractor(ITwitterTweetsService twitterTweetsService)
        {
            _twitterTweetsService = twitterTweetsService;
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
                    var extracted = _twitterTweetsService.GetTweet(statusId);
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
                MessageContent = ExtractMessage(tweet),
                Media = ExtractMedia(tweet),
                CreatedAt = DateTime.Now, // tweet.GetProperty("data").GetProperty("in_reply_to_status_id").GetDateTime(),
                IsReply = IsReply,
                IsThread = false,
                IsRetweet = IsRetweet,
                RetweetUrl = ExtractRetweetUrl(tweet)
            };

            return extractedTweet;
        }

        private string ExtractRetweetUrl(JsonElement tweet)
        {
            var retweetId = "123";
            return $"https://t.co/{retweetId}";

        }

        private string ExtractMessage(JsonElement tweet)
        {
            return tweet.GetProperty("text").GetString();
            //var message = tweet.FullText;
            //var tweetUrls = tweet.Media.Select(x => x.URL).Distinct();
            
            //if (tweet.IsRetweet && message.StartsWith("RT") && tweet.RetweetedTweet != null)
            //{
            //    message = tweet.RetweetedTweet.FullText;
            //    tweetUrls = tweet.RetweetedTweet.Media.Select(x => x.URL).Distinct();
            //}

            //foreach (var tweetUrl in tweetUrls)
            //{
            //    if(tweet.IsRetweet)
            //        message = tweet.RetweetedTweet.FullText.Replace(tweetUrl, string.Empty).Trim();
            //    else 
            //        message = message.Replace(tweetUrl, string.Empty).Trim();
            //}

            //if (tweet.QuotedTweet != null) message = $"[Quote {{RT}}]{Environment.NewLine}{message}";
            //if (tweet.IsRetweet)
            //{
            //    if (tweet.RetweetedTweet != null && !message.StartsWith("RT"))
            //        message = $"[{{RT}} @{tweet.RetweetedTweet.CreatedBy.ScreenName}]{Environment.NewLine}{message}";
            //    else if (tweet.RetweetedTweet != null && message.StartsWith($"RT @{tweet.RetweetedTweet.CreatedBy.ScreenName}:"))
            //        message = message.Replace($"RT @{tweet.RetweetedTweet.CreatedBy.ScreenName}:", $"[{{RT}} @{tweet.RetweetedTweet.CreatedBy.ScreenName}]{Environment.NewLine}");
            //    else
            //        message = message.Replace("RT", "[{{RT}}]");
            //}

            //// Expand URLs
            //foreach (var url in tweet.Urls.OrderByDescending(x => x.URL.Length))
            //    message = message.Replace(url.URL, url.ExpandedURL);

            //return message;
        }

        private ExtractedMedia[] ExtractMedia(JsonElement tweet)
        {
            //var media = tweet.Media;
            //if (tweet.IsRetweet && tweet.RetweetedTweet != null)
            //    media = tweet.RetweetedTweet.Media;

            //var result = new List<ExtractedMedia>();
            //foreach (var m in media)
            //{
            //    var mediaUrl = GetMediaUrl(m);
            //    var mediaType = GetMediaType(m.MediaType, mediaUrl);
            //    if (mediaType == null) continue;

            //    var att = new ExtractedMedia
            //    {
            //        MediaType = mediaType,
            //        Url = mediaUrl
            //    };
            //    result.Add(att);
            //}

            //return result.ToArray();
            return Array.Empty<ExtractedMedia>();
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
