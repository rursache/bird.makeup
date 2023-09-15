using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Tools;
using BirdsiteLive.Statistics.Domain;
using Moq;
using System.Net.Http;
using BirdsiteLive.Common.Settings;

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
            var httpFactory = new Mock<IHttpClientFactory>();
            var settings = new InstanceSettings
            {
                Domain = "domain.name"
            };
            httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
            ITwitterAuthenticationInitializer auth = new TwitterAuthenticationInitializer(httpFactory.Object, settings, logger1.Object);
            _tweetService = new TwitterUserService(auth, stats.Object, logger3.Object);
        }

        [TestMethod]
        public async Task UserKobe()
        {
            var user = await _tweetService.GetUserAsync("kobebryant");
            Assert.AreEqual(user.Name, "Kobe Bryant");
            Assert.AreEqual(user.Acct, "kobebryant");
        }

        [TestMethod]
        public async Task UserGrant()
        {
            var user = await _tweetService.GetUserAsync("grantimahara");
            Assert.AreEqual(user.Name, "Grant Imahara");
            Assert.AreEqual(user.StatusCount, 12496);
            Assert.AreEqual(user.Acct, "grantimahara");
        }

    }
}
