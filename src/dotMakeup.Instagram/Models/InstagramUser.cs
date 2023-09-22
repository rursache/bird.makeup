using BirdsiteLive.Common.Interfaces;

namespace dotMakeup.Instagram.Models;

public class InstagramUser : SocialMediaUser
{
    
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Protected { get; set; }
        public string Description { get; set; }
        public IEnumerable<long> PinnedPosts { get; set; }
        public string Url { get; set; }
        public string Acct { get; set; }
        public string ProfileImageUrl { get; set; }
        public string ProfileBannerURL { get; set; }
}