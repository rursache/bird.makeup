﻿using System;
using System.Collections.Generic;
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

                if (_instanceSettings.ParallelTwitterRequests == 0)
                {
                    while (true)
                        await Task.Delay(10000);
                }
                
                var usersDal = await _twitterUserDal.GetAllTwitterUsersWithFollowersAsync(2000, _instanceSettings.n_start, _instanceSettings.n_end, _instanceSettings.m);

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
