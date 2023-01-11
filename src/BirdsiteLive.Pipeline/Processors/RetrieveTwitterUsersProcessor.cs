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
using BirdsiteLive.Pipeline.Tools;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Pipeline.Processors
{
    public class RetrieveTwitterUsersProcessor : IRetrieveTwitterUsersProcessor
    {
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly IMaxUsersNumberProvider _maxUsersNumberProvider;
        private readonly ILogger<RetrieveTwitterUsersProcessor> _logger;
        private static Random rng = new Random();
        
        public int WaitFactor = 1000 * 60; //1 min

        #region Ctor
        public RetrieveTwitterUsersProcessor(ITwitterUserDal twitterUserDal, IMaxUsersNumberProvider maxUsersNumberProvider, ILogger<RetrieveTwitterUsersProcessor> logger)
        {
            _twitterUserDal = twitterUserDal;
            _maxUsersNumberProvider = maxUsersNumberProvider;
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
                    var users = await _twitterUserDal.GetAllTwitterUsersWithFollowersAsync(500);

                    var userCount = users.Any() ? Math.Min(users.Length, 25) : 1;
                    var splitUsers = users.OrderBy(a => rng.Next()).ToArray().Split(userCount).ToList();

                    foreach (var u in splitUsers)
                    {
                        ct.ThrowIfCancellationRequested();
                        UserWithDataToSync[] toSync = u.Select(x => new UserWithDataToSync { User = x }).ToArray();

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
