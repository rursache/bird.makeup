using System;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Models;
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
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;
        private readonly MemoryCacheEntryOptions _cacheEntryOptionsError;

        #region Ctor
        public CachedTwitterTweetsService(ITwitterTweetsService twitterService, InstanceSettings settings)
        {
            _twitterService = twitterService;

            _tweetCache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = settings.TweetCacheCapacity,
            });
            _cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1)
                //Priority on removing when reaching size limit (memory pressure)
                .SetPriority(CacheItemPriority.Low)
                // Keep in cache for this time, reset time if accessed.
                .SetSlidingExpiration(TimeSpan.FromDays(1))
                // Remove from cache after this time, regardless of sliding expiration
                .SetAbsoluteExpiration(TimeSpan.FromDays(2));
            _cacheEntryOptionsError = new MemoryCacheEntryOptions()
                .SetSize(1)
                //Priority on removing when reaching size limit (memory pressure)
                .SetPriority(CacheItemPriority.Low)
                // Keep in cache for this time, reset time if accessed.
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                // Remove from cache after this time, regardless of sliding expiration
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        }
        #endregion

        public async Task<ExtractedTweet[]> GetTimelineAsync(SyncTwitterUser user, long id)
        {
            var res = await _twitterService.GetTimelineAsync(user, id);
            return res;
        }
        public async Task<ExtractedTweet> GetTweetAsync(long id)
        {
            if (!_tweetCache.TryGetValue(id, out Task<ExtractedTweet> tweet))
            {
                tweet = _twitterService.GetTweetAsync(id);
                
                if (tweet is null)
                    await _tweetCache.Set(id, tweet, _cacheEntryOptionsError);
                else
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