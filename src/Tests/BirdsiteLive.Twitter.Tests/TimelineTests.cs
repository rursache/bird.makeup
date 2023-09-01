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
        private ICachedTwitterUserService _twitterUserService;
        private ITwitterUserDal _twitterUserDalMoq;

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
                .ReturnsAsync((string username) => new SyncTwitterUser { Acct = username, TwitterUserId = default });
            _twitterUserDalMoq = twitterDal.Object;

            ITwitterAuthenticationInitializer auth = new TwitterAuthenticationInitializer(httpFactory.Object, settings, logger1.Object);
            ITwitterUserService user = new TwitterUserService(auth, stats.Object, logger2.Object);
            _twitterUserService = new CachedTwitterUserService(user, settings);
            _tweetService = new TwitterTweetsService(auth, stats.Object, _twitterUserService, twitterDal.Object, settings, httpFactory.Object, logger3.Object);

        }

        [TestMethod]
        public async Task TimelineKobe()
        {
            var user = await _twitterUserDalMoq.GetTwitterUserAsync("kobebryant");
            var tweets = await _tweetService.GetTimelineAsync(user, 1117506566939234304);
            
            if (tweets.Length == 0)
                Assert.Inconclusive();
           
            Assert.AreEqual(tweets.Length, 13);
            Assert.AreEqual(tweets[0].MessageContent, "Continuing to move the game forward @KingJames. Much respect my brother 💪🏾 #33644");
            Assert.IsTrue(tweets.Length < 20);

            
            Assert.IsTrue(_twitterUserService.UserIsCached("kobebryant"));
            bool aRetweetedAccountIsCached = _twitterUserService.UserIsCached("djvlad");
            Assert.IsTrue(aRetweetedAccountIsCached);
            
        }

        [TestMethod]
        public async Task TimelineGrant()
        {
            var user = await _twitterUserDalMoq.GetTwitterUserAsync("grantimahara");
            user.Followers = 99999999; // we want to make sure it's a VIP user
            var tweets = await _tweetService.GetTimelineAsync(user, 1232042440875335680);

            Assert.AreEqual(tweets.Length, 19);
            
            Assert.IsTrue(tweets[0].IsReply);
            Assert.IsFalse(tweets[0].IsRetweet);
 
            Assert.AreEqual(tweets[2].MessageContent, "Liftoff!");
            Assert.IsTrue(tweets[2].IsRetweet);
            Assert.AreEqual(tweets[2].RetweetId, 1266812530833240064); 
            //Assert.AreEqual(tweets[2].Id, 1266813644626489345);
            Assert.AreEqual(tweets[2].OriginalAuthor.Acct, "spacex");
            Assert.AreEqual(tweets[2].Author.Acct, "grantimahara");
        }

    }
}
