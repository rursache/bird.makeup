﻿using BirdsiteLive.Common.Interfaces;

namespace BirdsiteLive.Twitter.Models
{
    public class TwitterUser : SocialMediaUser
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ProfileImageUrl { get; set; }
        public string ProfileBackgroundImageUrl { get; set; }
        public string Acct { get; set; }
        public string ProfileBannerURL { get; set; }
        public bool Protected { get; set; }
    }
}