﻿namespace BirdsiteLive.Models
{
    public class DisplayTwitterUser
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Acct { get; set; }
        public string Url { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool Protected { get; set; }
        public int FollowerCount { get; set; }
        public string MostPopularServer { get; set; }

        public string InstanceHandle { get; set; }
        public string FediverseAccount { get; set; }
    }
}