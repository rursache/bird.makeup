using System;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BirdsiteLive.Twitter
{
    public interface ICachedTwitterUserService : ITwitterUserService
    {
        void PurgeUser(string username);
        void AddUser(TwitterUser user);
        bool UserIsCached(string username);
    }

    public class CachedTwitterUserService : ICachedTwitterUserService
    {
        private readonly ITwitterUserService _twitterService;

        private readonly MemoryCache _userCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(1)//Size amount
            //Priority on removing when reaching size limit (memory pressure)
            .SetPriority(CacheItemPriority.Low)
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromDays(1))
            // Remove from cache after this time, regardless of sliding expiration
            .SetAbsoluteExpiration(TimeSpan.FromDays(2));

        private readonly MemoryCacheEntryOptions _cacheEntryOptionsError = new MemoryCacheEntryOptions()
            .SetSize(1)//Size amount
            //Priority on removing when reaching size limit (memory pressure)
            .SetPriority(CacheItemPriority.Low)
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            // Remove from cache after this time, regardless of sliding expiration
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        #region Ctor
        public CachedTwitterUserService(ITwitterUserService twitterService, InstanceSettings settings)
        {
            _twitterService = twitterService;

            _userCache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = settings.UserCacheCapacity
            });
        }
        #endregion

        public bool UserIsCached(string username)
        {
            return _userCache.TryGetValue(username, out _);
        }
        public async Task<TwitterUser> GetUserAsync(string username)
        {
            if (!_userCache.TryGetValue(username, out TwitterUser user))
            {
                user = await _twitterService.GetUserAsync(username);
                if (user is null)
                    _userCache.Set(username, user, _cacheEntryOptionsError);
                else
                    _userCache.Set(username, user, _cacheEntryOptions);
            }

            return user;
        }

        public bool IsUserApiRateLimited()
        {
            return _twitterService.IsUserApiRateLimited();
        }

        public TwitterUser Extract(JsonElement result)
        {
            var extract = _twitterService.Extract(result);
            _userCache.Set(extract.Acct, extract, _cacheEntryOptions);
            return extract;
        }
        public void PurgeUser(string username)
        {
            _userCache.Remove(username);
        }
        public void AddUser(TwitterUser user)
        {

            _userCache.Set(user.Acct, user, _cacheEntryOptions);
        }
    }
}