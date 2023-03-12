using System;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BirdsiteLive.Twitter
{
    public interface ICachedTwitterTweetsService : ITwitterTweetsService
    {
        void SetTweet(long id, ExtractedTweet tweet);
    }

    public class CachedTwitterTweetsService : ICachedTwitterTweetsService
    {
        private readonly ITwitterTweetsService _twitterService;

        private readonly MemoryCache _tweetCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(10000)//Size amount
            //Priority on removing when reaching size limit (memory pressure)
            .SetPriority(CacheItemPriority.Low)
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromMinutes(60))
            // Remove from cache after this time, regardless of sliding expiration
            .SetAbsoluteExpiration(TimeSpan.FromDays(1));

        #region Ctor
        public CachedTwitterTweetsService(ITwitterTweetsService twitterService, InstanceSettings settings)
        {
            _twitterService = twitterService;

            _tweetCache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = 10000 //TODO make this use number of entries in db
            });
        }
        #endregion

        public async Task<ExtractedTweet[]> GetTimelineAsync(string username, long id)
        {
            var res = await _twitterService.GetTimelineAsync(username, id);
            return res;
        }
        public async Task<ExtractedTweet> GetTweetAsync(long id)
        {
            if (!_tweetCache.TryGetValue(id, out Task<ExtractedTweet> tweet))
            {
                tweet = _twitterService.GetTweetAsync(id);
                await _tweetCache.Set(id, tweet, _cacheEntryOptions);
            }

            return await tweet;
        }

        public void SetTweet(long id, ExtractedTweet tweet)
        {

            _tweetCache.Set(id, tweet, _cacheEntryOptions);
        }
    }
}