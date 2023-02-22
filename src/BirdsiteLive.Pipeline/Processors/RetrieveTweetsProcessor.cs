using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Pipeline.Contracts;
using BirdsiteLive.Pipeline.Models;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Models;
using BirdsiteLive.Common.Settings;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Pipeline.Processors.SubTasks
{
    public class RetrieveTweetsProcessor : IRetrieveTweetsProcessor
    {
        private readonly ITwitterTweetsService _twitterTweetsService;
        private readonly ICachedTwitterUserService _twitterUserService;
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly ILogger<RetrieveTweetsProcessor> _logger;
        private readonly InstanceSettings _settings;

        #region Ctor
        public RetrieveTweetsProcessor(ITwitterTweetsService twitterTweetsService, ITwitterUserDal twitterUserDal, ICachedTwitterUserService twitterUserService, InstanceSettings settings, ILogger<RetrieveTweetsProcessor> logger)
        {
            _twitterTweetsService = twitterTweetsService;
            _twitterUserDal = twitterUserDal;
            _twitterUserService = twitterUserService;
            _logger = logger;
            _settings = settings;
        }
        #endregion

        public async Task<UserWithDataToSync[]> ProcessAsync(UserWithDataToSync[] syncTwitterUsers, CancellationToken ct)
        {

            if (_settings.ParallelTwitterRequests == 0)
            {
                while(true)
                    await Task.Delay(1000);
            }

            var usersWtTweets = new ConcurrentBag<UserWithDataToSync>();
            List<Task> todo = new List<Task>();
            int index = 0;
            foreach (var userWtData in syncTwitterUsers)
            {
                index++;

                var t = Task.Run(async () => {
                    try 
                    {
                        var user = userWtData.User;
                        var tweets = await RetrieveNewTweets(user);
                        _logger.LogInformation(index + "/" + syncTwitterUsers.Count() + " Got " + tweets.Length + " tweets from user " + user.Acct + " " );
                        if (tweets.Length > 0 && user.LastTweetPostedId != -1)
                        {
                            userWtData.Tweets = tweets;
                            usersWtTweets.Add(userWtData);
                        }
                        else if (tweets.Length > 0 && user.LastTweetPostedId == -1)
                        {
                            var tweetId = tweets.Last().Id;
                            var now = DateTime.UtcNow;
                            await _twitterUserDal.UpdateTwitterUserAsync(user.Id, tweetId, tweetId, user.FetchingErrorCount, now);
                        }
                        else
                        {
                            var now = DateTime.UtcNow;
                            await _twitterUserDal.UpdateTwitterUserAsync(user.Id, user.LastTweetPostedId, user.LastTweetSynchronizedForAllFollowersId, user.FetchingErrorCount, now);
                        }

                    } 
                    catch(Exception e)
                    {
                        _logger.LogError(e.Message);

                    }
                });
                todo.Add(t);
                if (todo.Count > _settings.ParallelTwitterRequests)
                {
                    await Task.WhenAll(todo);
                    todo.Clear();
                }
                
            }

            await Task.WhenAll(todo);
            return usersWtTweets.ToArray();
        }

        private async Task<ExtractedTweet[]> RetrieveNewTweets(SyncTwitterUser user)
        {
            var tweets = new ExtractedTweet[0];
            
            try
            {
                if (user.LastTweetPostedId == -1)
                    tweets = await _twitterTweetsService.GetTimelineAsync(user.Acct);
                else
                    tweets = await _twitterTweetsService.GetTimelineAsync(user.Acct, user.LastTweetSynchronizedForAllFollowersId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving TL of {Username} from {LastTweetPostedId}, purging user from cache", user.Acct, user.LastTweetPostedId);
                _twitterUserService.PurgeUser(user.Acct);
            }

            return tweets;
        }
    }
}