namespace BirdsiteLive.Common.Settings
{
    public class InstanceSettings
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public string AdminEmail { get; set; }
        public bool ResolveMentionsInProfiles { get; set; }
        public bool PublishReplies { get; set; }

        public string UnlistedTwitterAccounts { get; set; }
        public string SensitiveTwitterAccounts { get; set; }

        public int FailingTwitterUserCleanUpThreshold { get; set; }
        public int FailingFollowerCleanUpThreshold { get; set; } = -1;

        public int UserCacheCapacity { get; set; } = 40_000;
        public int TweetCacheCapacity { get; set; } = 20_000;
        // "AAAAAAAAAAAAAAAAAAAAAPYXBAAAAAAACLXUNDekMxqa8h%2F40K4moUkGsoc%3DTYfbDKbT3jJPCEVnMYqilB28NHfOPqkca3qaAxGfsyKCs0wRbw"
        public string TwitterBearerToken { get; set; } = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";
        public int m { get; set; } = 1;
        public int n_start { get; set; } = 0;
        public int n_end { get; set; } = 1;
        public int ParallelTwitterRequests { get; set; } = 10;
        public int ParallelFediversePosts { get; set; } = 10;
    }
}
