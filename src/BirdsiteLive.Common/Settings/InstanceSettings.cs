using System;

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
        public int m { get; set; } = 1;
        public int n_start { get; set; } = 0;
        public int n_end { get; set; } = 1;
        public bool MultiplyNByOrdinal { get; set; } = false;

        public string MachineName { get; set; } = Environment.MachineName;
        public int ParallelTwitterRequests { get; set; } = 10;
        public int TwitterRequestDelay { get; set; } = 0;
        public int ParallelFediversePosts { get; set; } = 10;
        public int PipelineStartupDelay { get; set; } = 5 * 60;
    }
}
