using System;
using System.Text.Json;
using System.Threading.Tasks;
using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BirdsiteLive.Twitter
{

    public class TwitterService : ISocialMediaService
    {
        private readonly ITwitterTweetsService _twitterTweetsService;
        private readonly ITwitterUserService _twitterUserService;

        #region Ctor
        public TwitterService(ICachedTwitterTweetsService twitterService, ICachedTwitterUserService twitterUserService, InstanceSettings settings)
        {
            _twitterTweetsService = twitterService;
            _twitterUserService = twitterUserService;
        }
        #endregion

        public async Task<SocialMediaUser> GetUserAsync(string user)
        {
            var res = await _twitterUserService.GetUserAsync(user);
            return res;
        }

    }
}