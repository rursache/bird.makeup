using BirdsiteLive.Common.Interfaces;

namespace dotMakeup.Instagram.Models;

public class InstagramUser : SocialMediaUser
{
    
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
}