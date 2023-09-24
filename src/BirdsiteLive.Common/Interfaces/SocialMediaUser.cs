using System.Collections.Generic;

namespace BirdsiteLive.Common.Interfaces;

public interface SocialMediaUser
{
        public long Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Acct { get; set; }
        public string ProfileImageUrl { get; set; }
        public string ProfileBannerURL{ get; set; }
        public bool Protected { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public IEnumerable<long> PinnedPosts { get; set; }
}