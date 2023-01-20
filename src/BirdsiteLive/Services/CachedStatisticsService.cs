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

        private static CachedStatistics _cachedStatistics;
        private readonly InstanceSettings _instanceSettings;

        #region Ctor
        public CachedStatisticsService(ITwitterUserDal twitterUserDal, IFollowersDal followersDal, InstanceSettings instanceSettings)
        {
            _twitterUserDal = twitterUserDal;
            _instanceSettings = instanceSettings;
            _followersDal = followersDal;
        }
        #endregion

        public async Task<CachedStatistics> GetStatisticsAsync()
        {
            if (_cachedStatistics == null ||
                (DateTime.UtcNow - _cachedStatistics.RefreshedTime).TotalMinutes > 15)
            {
                var twitterUserCount = await _twitterUserDal.GetTwitterUsersCountAsync();
                var twitterSyncLag = await _twitterUserDal.GetTwitterSyncLag();
                var fediverseUsers = await _followersDal.GetFollowersCountAsync();

                _cachedStatistics = new CachedStatistics
                {
                    RefreshedTime = DateTime.UtcNow,
                    SyncLag = twitterSyncLag,
                    TwitterUsers = twitterUserCount,
                    FediverseUsers = fediverseUsers
                };
            }

            return _cachedStatistics;
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