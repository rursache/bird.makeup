using System.Net.Http;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Domain.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;


namespace BirdsiteLive.Domain.Tests
{
    [TestClass]
    public class ActivityServiceTests
    {
        private readonly InstanceSettings _settings;

        #region Ctor
        public ActivityServiceTests()
        {
            _settings = new InstanceSettings
            {
                Domain = "domain.name"
            };
        }
        #endregion

        [TestMethod]
        public async Task ActivityTest()
        {
            var logger1 = new Mock<ILogger<ActivityPubService>>();
            var httpFactory = new Mock<IHttpClientFactory>();
            var keyFactory = new Mock<MagicKeyFactory>();
            var cryptoService = new CryptoService(keyFactory.Object);
            httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
            var service = new ActivityPubService(cryptoService, _settings, httpFactory.Object, logger1.Object);

            var activity = new ActivityAcceptFollow()
            {
                id = "awef",
            };
            var json = "{\"id\":\"awef\"}";
            #region Validations

            var req = service.BuildRequest(activity, "google.com", "tata", "awef");
            
            Assert.AreEqual(await req.Content.ReadAsStringAsync(), json);

            #endregion
        }
        [TestMethod]
        public async Task AcceptFollow()
        {
 

            var logger1 = new Mock<ILogger<ActivityPubService>>();
            var httpFactory = new Mock<IHttpClientFactory>();
            var keyFactory = new Mock<MagicKeyFactory>();
            var cryptoService = new CryptoService(keyFactory.Object);
            httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
            var service = new ActivityPubService(cryptoService, _settings, httpFactory.Object, logger1.Object);

            var json = "{ \"@context\":\"https://www.w3.org/ns/activitystreams\",\"id\":\"https://mastodon.technology/c94567cf-1fda-42ba-82fc-a0f82f63ccbe\",\"type\":\"Follow\",\"actor\":\"https://mastodon.technology/users/testtest\",\"object\":\"https://4a120ca2680e.ngrok.io/users/manu\"}";
            var activity = ApDeserializer.ProcessActivity(json) as ActivityFollow;

            var jsonres =
                "{\"object\":{\"id\":\"https://mastodon.technology/c94567cf-1fda-42ba-82fc-a0f82f63ccbe\",\"type\":\"Follow\",\"actor\":\"https://mastodon.technology/users/testtest\",\"object\":\"https://4a120ca2680e.ngrok.io/users/manu\"},\"@context\":\"https://www.w3.org/ns/activitystreams\",\"id\":\"https://4a120ca2680e.ngrok.io/users/manu#accepts/follows/32e5fbda-9159-4ede-8249-9d008092d26f\",\"type\":\"Accept\",\"actor\":\"https://4a120ca2680e.ngrok.io/users/manu\"}";
            var activityRes = ApDeserializer.ProcessActivity(jsonres) as ActivityAcceptFollow;
            #region Validations

            var req = service.BuildAcceptFollow(activity);
            
            Assert.AreEqual(req.actor, activityRes.actor);
            Assert.AreEqual(req.context, activityRes.context);

            #endregion
        }

    }
}
