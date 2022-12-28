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
    public class UserTests
    {
        private ITwitterUserService _tweetService;
        [TestInitialize]
        public async Task TestInit()
        {
            var logger1 = new Mock<ILogger<TwitterAuthenticationInitializer>>(MockBehavior.Strict);
            var logger2 = new Mock<ILogger<TwitterUserService>>(MockBehavior.Strict);
            var logger3 = new Mock<ILogger<TwitterUserService>>();
            var stats = new Mock<ITwitterStatisticsHandler>();
            ITwitterAuthenticationInitializer auth = new TwitterAuthenticationInitializer(logger1.Object);
            _tweetService = new TwitterUserService(auth, stats.Object, logger3.Object);
        }

        [TestMethod]
        public async Task TimelineKobe()
        {
            var user = await _tweetService.GetUserAsync("kobebryant");
            Assert.AreEqual(user.Name, "Kobe Bryant");
        }


    }
}
