using System;

namespace BirdsiteLive.DAL.Models
{
    public class SyncTwitterUser
    {
        public int Id { get; set; }
        public long TwitterUserId { get; set; }
        public string Acct { get; set; }
        public string FediAcct { get; set; }

        public long LastTweetPostedId { get; set; }

        public DateTime LastSync { get; set; }

        public int FetchingErrorCount { get; set; } //TODO: update DAL
        public long Followers { get; set; } 
    }
}