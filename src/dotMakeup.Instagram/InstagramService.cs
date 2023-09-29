using BirdsiteLive.Common.Interfaces;
using BirdsiteLive.Common.Settings;

namespace dotMakeup.Instagram;

public class InstagramService : ISocialMediaService
{
        private readonly InstagramUserService _userService;

        #region Ctor
        public InstagramService(InstagramUserService userService, InstanceSettings settings)
        {
            _userService = userService;
        }
        #endregion

        public async Task<SocialMediaUser> GetUserAsync(string username)
        {
            var user = await _userService.GetUserAsync(username);
            return user;
        }
}