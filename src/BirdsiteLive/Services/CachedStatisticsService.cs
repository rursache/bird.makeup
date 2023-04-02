using System;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;

namespace BirdsiteLive.Services
{
    public interface ICachedStatisticsService
    {
        Task<CachedStatistics> GetStatisticsAsync();
    }

    public class CachedStatisticsService : ICachedStatisticsService
    {
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly IFollowersDal _followersDal;

        private static Task<CachedStatistics> _cachedStatistics;
        private readonly InstanceSettings _instanceSettings;

        #region Ctor
        public CachedStatisticsService(ITwitterUserDal twitterUserDal, IFollowersDal followersDal, InstanceSettings instanceSettings)
        {
            _twitterUserDal = twitterUserDal;
            _instanceSettings = instanceSettings;
            _followersDal = followersDal;
            _cachedStatistics = CreateStats();
        }
        #endregion

        public async Task<CachedStatistics> GetStatisticsAsync()
        {
            var stats = await _cachedStatistics;
            if ((DateTime.UtcNow - stats.RefreshedTime).TotalMinutes > 5)
            {
                _cachedStatistics = CreateStats();
            }

            return stats;
        }

        private async Task<CachedStatistics> CreateStats()
        {
            var twitterUserCount = await _twitterUserDal.GetTwitterUsersCountAsync();
            var twitterSyncLag = await _twitterUserDal.GetTwitterSyncLag();
            var fediverseUsers = await _followersDal.GetFollowersCountAsync();

            var stats = new CachedStatistics
            {
                RefreshedTime = DateTime.UtcNow,
                SyncLag = twitterSyncLag,
                TwitterUsers = twitterUserCount,
                FediverseUsers = fediverseUsers
            };
        
            return stats;
        }
    }

    public class CachedStatistics
    {
        public DateTime RefreshedTime { get; set; }
        public TimeSpan SyncLag { get; set; }
        public int TwitterUsers { get; set; }
        public int FediverseUsers { get; set; }
    }
}