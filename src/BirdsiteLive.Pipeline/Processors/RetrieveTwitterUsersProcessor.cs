using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BirdsiteLive.Common.Extensions;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.Pipeline.Models;
using BirdsiteLive.Pipeline.Contracts;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Pipeline.Processors
{
    public class RetrieveTwitterUsersProcessor : IRetrieveTwitterUsersProcessor
    {
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly IFollowersDal _followersDal;
        private readonly InstanceSettings _instanceSettings;
        private readonly ILogger<RetrieveTwitterUsersProcessor> _logger;
        private static Random rng = new Random();
        
        public int WaitFactor = 1000 * 60; //1 min

        #region Ctor
        public RetrieveTwitterUsersProcessor(ITwitterUserDal twitterUserDal, IFollowersDal followersDal, InstanceSettings instanceSettings, ILogger<RetrieveTwitterUsersProcessor> logger)
        {
            _twitterUserDal = twitterUserDal;
            _followersDal = followersDal;
            _instanceSettings = instanceSettings;
            _logger = logger;
        }
        #endregion

        public async Task GetTwitterUsersAsync(BufferBlock<UserWithDataToSync[]> twitterUsersBufferBlock, CancellationToken ct)
        {
            for (; ; )
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var users = await _twitterUserDal.GetAllTwitterUsersWithFollowersAsync(2000, _instanceSettings.n_start, _instanceSettings.n_end, _instanceSettings.m);

                    var userCount = users.Any() ? Math.Min(users.Length, 200) : 1;
                    var splitUsers = users.OrderBy(a => rng.Next()).ToArray().Split(userCount).ToList();

                    foreach (var u in splitUsers)
                    {
                        ct.ThrowIfCancellationRequested();
                        UserWithDataToSync[] toSync = await Task.WhenAll(
                            u.Select(async x => new UserWithDataToSync
                                { User = x, Followers = await _followersDal.GetFollowersAsync(x.Id) } 
                            )
                        );

                        await twitterUsersBufferBlock.SendAsync(toSync, ct);
                    }

                    await Task.Delay(10, ct); // this is somehow necessary
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failing retrieving Twitter Users.");
                }
            }
        }
    }
}
