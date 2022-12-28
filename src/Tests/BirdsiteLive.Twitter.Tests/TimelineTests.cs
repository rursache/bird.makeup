using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Tools;
using BirdsiteLive.Statistics.Domain;
using Moq;

namespace BirdsiteLive.ActivityPub.Tests
{
    [TestClass]
    public class TimelineTests
    {
        private ITwitterTweetsService _tweetService;
        [TestInitialize]
        public async Task TestInit()
        {
            var logger1 = new Mock<ILogger<TwitterAuthenticationInitializer>>(MockBehavior.Strict);
            var logger2 = new Mock<ILogger<TwitterUserService>>(MockBehavior.Strict);
            var logger3 = new Mock<ILogger<TwitterTweetsService>>();
            var stats = new Mock<ITwitterStatisticsHandler>();
            var settings = new Mock<Common.Settings.InstanceSettings>();
            ITwitterAuthenticationInitializer auth = new TwitterAuthenticationInitializer(logger1.Object);
            ITwitterUserService user = new TwitterUserService(auth, stats.Object, logger2.Object);
            ICachedTwitterUserService user2 = new CachedTwitterUserService(user, settings.Object);
            _tweetService = new TwitterTweetsService(auth, stats.Object, user2, logger3.Object);
        }

        [TestMethod]
        public async Task TimelineKobe()
        {
            var tweets = await _tweetService.GetTimelineAsync("kobebryant", 100, 1218020971346444288);
            Assert.AreEqual(tweets[0].MessageContent, "Continuing to move the game forward @KingJames. Much respect my brother 💪🏾 #33644");
            Assert.AreEqual(tweets.Length, 9);
        }


    }
}
