using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Tools;
using BirdsiteLive.Statistics.Domain;
using BirdsiteLive.Common.Settings;
using Moq;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using System.Net.Http;

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
            var twitterDal = new Mock<ITwitterUserDal>();
            var httpFactory = new Mock<IHttpClientFactory>();
            httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
            var settings = new InstanceSettings
            {
                Domain = "domain.name"
            };

            twitterDal
                .Setup(x => x.GetTwitterUserAsync(
                    It.Is<string>(y => true)
                ))
                .ReturnsAsync(new SyncTwitterUser { TwitterUserId = default });

            ITwitterAuthenticationInitializer auth = new TwitterAuthenticationInitializer(httpFactory.Object, settings, logger1.Object);
            ITwitterUserService user = new TwitterUserService(auth, stats.Object, logger2.Object);
            ICachedTwitterUserService user2 = new CachedTwitterUserService(user, settings);
            _tweetService = new TwitterTweetsService(auth, stats.Object, user2, twitterDal.Object, settings, logger3.Object);

        }

        [TestMethod]
        public async Task TimelineKobe()
        {
            var tweets = await _tweetService.GetTimelineAsync("kobebryant", 1218020971346444288);
            Assert.AreEqual(tweets[0].MessageContent, "Continuing to move the game forward @KingJames. Much respect my brother 💪🏾 #33644");
            Assert.IsTrue(tweets.Length > 5);
        }

        [TestMethod]
        [Ignore]
        public async Task TimelineGrant()
        {
            var tweets = await _tweetService.GetTimelineAsync("grantimahara", default);
            Assert.IsTrue(tweets[0].IsReply);
            Assert.IsTrue(tweets.Length > 10);

            Assert.AreEqual(tweets[2].MessageContent, "Liftoff!");
            Assert.AreEqual(tweets[2].RetweetId, 1266812530833240064);
            Assert.AreEqual(tweets[2].Id, 1266813644626489345);
            Assert.AreEqual(tweets[2].OriginalAuthor.Acct, "SpaceX");
            Assert.AreEqual(tweets[2].Author.Acct, "grantimahara");
            Assert.IsTrue(tweets[2].IsRetweet);
        }

    }
}
