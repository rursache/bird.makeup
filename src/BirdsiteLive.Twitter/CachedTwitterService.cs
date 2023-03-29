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
            .SetSlidingExpiration(TimeSpan.FromMinutes(60))
            // Remove from cache after this time, regardless of sliding expiration
            .SetAbsoluteExpiration(TimeSpan.FromDays(1));

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
            if (!_userCache.TryGetValue(username, out Task<TwitterUser> user))
            {
                user = _twitterService.GetUserAsync(username);
                await _userCache.Set(username, user, _cacheEntryOptions);
            }

            return await user;
        }

        public bool IsUserApiRateLimited()
        {
            return _twitterService.IsUserApiRateLimited();
        }

        public TwitterUser Extract(JsonElement result)
        {
            return _twitterService.Extract(result);
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