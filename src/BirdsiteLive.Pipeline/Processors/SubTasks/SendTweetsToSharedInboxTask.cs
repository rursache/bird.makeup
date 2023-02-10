using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Domain;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Pipeline.Processors.SubTasks
{
    public interface ISendTweetsToSharedInboxTask
    {
        Task ExecuteAsync(ExtractedTweet[] tweets, SyncTwitterUser user, string host, Follower[] followersPerInstance);
    }

    public class SendTweetsToSharedInboxTask : ISendTweetsToSharedInboxTask
    {
        private readonly IStatusService _statusService;
        private readonly IActivityPubService _activityPubService;
        private readonly IFollowersDal _followersDal;
        private readonly InstanceSettings _settings;
        private readonly ILogger<SendTweetsToSharedInboxTask> _logger;

        #region Ctor
        public SendTweetsToSharedInboxTask(IActivityPubService activityPubService, IStatusService statusService, IFollowersDal followersDal, InstanceSettings settings, ILogger<SendTweetsToSharedInboxTask> logger)
        {
            _activityPubService = activityPubService;
            _statusService = statusService;
            _followersDal = followersDal;
            _settings = settings;
            _logger = logger;
        }
        #endregion

        public async Task ExecuteAsync(ExtractedTweet[] tweets, SyncTwitterUser user, string host, Follower[] followersPerInstance)
        {
            var userId = user.Id;
            var inbox = followersPerInstance.First().SharedInboxRoute;

            var fromStatusId = followersPerInstance
                .Max(x => x.FollowingsSyncStatus[userId]);

            var tweetsToSend = tweets
                .Where(x => x.Id > fromStatusId)
                .OrderBy(x => x.Id)
                .ToList();
            _logger.LogInformation("After filtering, there were " + tweetsToSend.Count() + " tweets left to send");

            var syncStatus = fromStatusId;
            try
            {
                foreach (var tweet in tweetsToSend)
                {
                    try
                    {
                        var activity = _statusService.GetActivity(user.Acct, tweet);
                        await _activityPubService.PostNewActivity(activity, user.Acct, tweet.Id.ToString(), host, inbox);
                    }
                    catch (ArgumentException e)
                    {
                        if (e.Message.Contains("Invalid pattern") && e.Message.Contains("at offset")) //Regex exception
                        {
                            _logger.LogError(e, "Can't parse {MessageContent} from Tweet {Id}", tweet.MessageContent, tweet.Id);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    syncStatus = tweet.Id;
                }
            }
            finally
            {
                if (syncStatus != fromStatusId)
                {
                    foreach (var f in followersPerInstance)
                    {
                        f.FollowingsSyncStatus[userId] = syncStatus;
                        await _followersDal.UpdateFollowerAsync(f);
                    }
                }
            }
        }
    }
}