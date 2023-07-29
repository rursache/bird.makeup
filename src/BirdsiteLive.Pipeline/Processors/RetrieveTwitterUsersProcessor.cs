using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        private readonly Regex _ordinalRegex = new Regex(".*-([0-9])");
        private readonly int _n_start;
        private readonly int _n_end;

        #region Ctor
        public RetrieveTwitterUsersProcessor(ITwitterUserDal twitterUserDal, IFollowersDal followersDal, InstanceSettings instanceSettings, ILogger<RetrieveTwitterUsersProcessor> logger)
        {
            _twitterUserDal = twitterUserDal;
            _followersDal = followersDal;
            _instanceSettings = instanceSettings;
            _logger = logger;

            if (_instanceSettings.MultiplyNByOrdinal)
            {
                var ordinal = int.Parse( _ordinalRegex.Match( _instanceSettings.MachineName ).Groups[1].Value );
                var range = _instanceSettings.n_end - _instanceSettings.n_start;
                _n_start = _instanceSettings.n_start + (range * ordinal);
                _n_end = _instanceSettings.n_end + (range * ordinal) - 1;
            }
            else
            {
                _n_start = _instanceSettings.n_start;
                _n_end = _instanceSettings.n_end;
            }
        }
        #endregion

        public async Task GetTwitterUsersAsync(BufferBlock<UserWithDataToSync[]> twitterUsersBufferBlock, CancellationToken ct)
        {
            for (; ; )
            {
                ct.ThrowIfCancellationRequested();

                if (_instanceSettings.ParallelTwitterRequests == 0)
                {
                    while (true)
                        await Task.Delay(10000);
                }
                
                var usersDal = await _twitterUserDal.GetAllTwitterUsersWithFollowersAsync(2000, _n_start, _n_end, _instanceSettings.m);

                var userCount = usersDal.Any() ? Math.Min(usersDal.Length, 200) : 1;
                var splitUsers = usersDal.OrderBy(a => rng.Next()).ToArray().Split(userCount).ToList();

                foreach (var users in splitUsers)
                {
                    ct.ThrowIfCancellationRequested();
                    List<UserWithDataToSync> toSync = new List<UserWithDataToSync>();
                    foreach (var u in users)
                    {
                        var followers = await _followersDal.GetFollowersAsync(u.Id);
                        toSync.Add( new UserWithDataToSync()
                        {
                            User = u,
                            Followers = followers
                        });
                        
                    }

                    await twitterUsersBufferBlock.SendAsync(toSync.ToArray(), ct);

                }
                
                await Task.Delay(10, ct); // this is somehow necessary
            }
        }
    }
}
