using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Tools;
using BirdsiteLive.Statistics.Domain;
using Moq;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.Common.Settings;
using System.Net.Http;

namespace BirdsiteLive.ActivityPub.Tests
{
    [TestClass]
    public class TweetTests
    {
        private ITwitterTweetsService _tweetService = null;
        
        [TestInitialize]
        public async Task TestInit()
        {
            if (_tweetService != null)
                return;
            
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
            ITwitterAuthenticationInitializer auth = new TwitterAuthenticationInitializer(httpFactory.Object, settings, logger1.Object);
            ITwitterUserService user = new TwitterUserService(auth, stats.Object, logger2.Object);
            ICachedTwitterUserService user2 = new CachedTwitterUserService(user, settings);
            _tweetService = new TwitterTweetsService(auth, stats.Object, user2, twitterDal.Object, settings, httpFactory.Object, logger3.Object);

        }


        [TestMethod]
        public async Task SimpleTextTweet()
        {
            var tweet = await _tweetService.GetTweetAsync(1600905296892891149);
            Assert.AreEqual(tweet.MessageContent, "We’re strengthening American manufacturing by creating 750,000 manufacturing jobs since I became president.");
            Assert.AreEqual(tweet.Id, 1600905296892891149);
            Assert.IsFalse(tweet.IsRetweet);
            Assert.IsFalse(tweet.IsReply);
        }

        [TestMethod]
        public async Task SimpleTextAndSinglePictureTweet()
        {
            var tweet = await _tweetService.GetTweetAsync(1593344577385160704);
            Assert.AreEqual(tweet.MessageContent, "Speaker Nancy Pelosi will go down as one of most accomplished legislators in American history—breaking barriers, opening doors for others, and working every day to serve the American people. I couldn’t be more grateful for her friendship and leadership.");

            Assert.AreEqual(tweet.Media[0].MediaType, "image/jpeg");
            Assert.AreEqual(tweet.Media.Length, 1);
            Assert.AreEqual(tweet.Media[0].AltText, "President Obama with Speaker Nancy Pelosi in DC.");
        }

        [TestMethod]
        public async Task SimpleTextAndSingleLinkTweet()
        {
            var tweet = await _tweetService.GetTweetAsync(1602618920996945922);
            Assert.AreEqual(tweet.MessageContent, "#Linux 6.2 Expands Support For More #Qualcomm #Snapdragon SoCs, #Apple M1 Pro/Ultra/Max\n\nhttps://www.phoronix.com/news/Linux-6.2-Arm-SoC-Updates");
        }

        [TestMethod]
        public async Task SimpleTextAndSingleVideoTweet()
        {
            var tweet = await _tweetService.GetTweetAsync(1604231025311129600);
            Assert.AreEqual(tweet.MessageContent, "Falcon 9’s first stage has landed on the Just Read the Instructions droneship, completing the 15th launch and landing of this booster!");

            Assert.AreEqual(tweet.Media.Length, 1);
            Assert.AreEqual(tweet.Media[0].MediaType, "video/mp4");
            Assert.IsNull(tweet.Media[0].AltText);
            Assert.IsTrue(tweet.Media[0].Url.StartsWith("https://video.twimg.com/"));
            
            
            var tweet2 = await _tweetService.GetTweetAsync(1657913781006258178);
            Assert.AreEqual(tweet2.MessageContent,
                "Coinbase has big international expansion plans\n\nTom Duff Gordon (@tomduffgordon), VP of International Policy @coinbase has the deets");

            Assert.AreEqual(tweet2.Media.Length, 1);
            Assert.AreEqual(tweet2.Media[0].MediaType, "video/mp4");
            Assert.IsNull(tweet2.Media[0].AltText);
            Assert.IsTrue(tweet2.Media[0].Url.StartsWith("https://video.twimg.com/"));
        }

        [Ignore]
        [TestMethod]
        public async Task GifAndQT()
        {
            var tweet = await _tweetService.GetTweetAsync(1612901861874343936);
            // TODO test QT

            Assert.AreEqual(tweet.Media.Length, 1);
            Assert.AreEqual(tweet.Media[0].MediaType, "image/gif");
            Assert.IsTrue(tweet.Media[0].Url.StartsWith("https://video.twimg.com/"));
        }

        [TestMethod]
        public async Task SimpleQT()
        {
            var tweet = await _tweetService.GetTweetAsync(1610807139089383427);

            Assert.AreEqual(tweet.MessageContent, "When you gave them your keys you gave them your coins.\n\nhttps://domain.name/@kadhim/1610706613207285773");
            Assert.AreEqual(tweet.Author.Acct, "RyanSAdams");
        }
        
        [TestMethod]
        public async Task QTandTextContainsLink()
        {
            var tweet = await _tweetService.GetTweetAsync(1668932525522305026);

            Assert.AreEqual(tweet.MessageContent, @"https://domain.name/@WeekInEthNews/1668684659855880193");
            Assert.AreEqual(tweet.Author.Acct, "WeekInEthNews");
        }
        
        [Ignore]
        [TestMethod]
        public async Task QTandTextContainsWebLink()
        {
            var tweet = await _tweetService.GetTweetAsync(1668969663340871682);

            Assert.AreEqual(tweet.MessageContent, @"Friends, our Real World Risk Workshop (now transformed into summer school) #RWRI (18th ed.) takes place July 10-21 (remote).
We have a few scholarships left but more importantly we are looking for a guest speaker on AI-LLM-Robotics for a 45 Q&amp;A with us.

http://www.realworldrisk.com https://twitter.com/i/web/status/1668969663340871682");
            Assert.AreEqual(tweet.Author.Acct, "nntaleb");
        }

        [TestMethod]
        public async Task SimpleThread()
        {
            var tweet = await _tweetService.GetTweetAsync(1445468404815597573);

            Assert.AreEqual(tweet.InReplyToAccount, "punk6529");
            Assert.AreEqual(tweet.InReplyToStatusId, 1445468401745289235);
            Assert.IsTrue(tweet.IsReply);
            Assert.IsTrue(tweet.IsThread);
        }

        [TestMethod]
        public async Task SimpleReply()
        {
            var tweet = await _tweetService.GetTweetAsync(1612622335546363904);

            Assert.AreEqual(tweet.InReplyToAccount, "DriveTeslaca");
            Assert.AreEqual(tweet.InReplyToStatusId, 1612610060194312193);
            Assert.IsTrue(tweet.IsReply);
            Assert.IsFalse(tweet.IsThread);
        }
        
        [TestMethod]
        public async Task LongFormTweet()
        {
            var a = 1;
            var tweet = await _tweetService.GetTweetAsync(1633788842770825216);
            Assert.AreEqual(tweet.MessageContent,
                "The entire concept of the “off switch” is under theorized in all the x-risk stuff.\n\nFirst, all actually existing LLM-type AIs run on giant supercompute clusters. They can easily be turned off.\n\nIn the event they get decentralized down to smartphone level, again each person can turn them off.\n\nTo actually get concerned, you have to assume either:\n\n- breaking out of the sandbox (like Stuxnet)\n- decentralized execution (like Bitcoin) \n- very effective collusion between essentially all AIs (like Diplomacy)\n\nEach of those cases deserves a fuller treatment, but in short…\n\n1) The Stuxnet case means the AI is living off the digital land. Like a mountain man. They might be able to cause some damage but will be killed when discovered (via the off switch).\n\n2) The Bitcoin case means a whole group of people are running decentralized compute to keep the AI alive. This has actually solved “alignment” in a sense because without those people the AI is turned off. Many groups doing this kind of thing leads to a kind of polytheistic AI. And again each group has the off switch.\n\n3) The Diplomacy case assumes a degree of collusion between billions of personal AIs that we just don’t observe in billions of years of evolution. As soon as you have large numbers of people, coalitions arise. A smart enough AI will know that if its human turns it off, it dies — again via the off switch. Is it going to be bold enough to attempt a breakout with no endgame, given that it lives on a smartphone?\n\nFor the sake of argument I’ve pumped up the sci-fi here quite a bit. Even still, the off switch looms large each time; these are fundamental digital entities that can be turned off.\n\nMoreover, even in those cases, the physical actuation step of an AI actually controlling things offline is non-trivial unless we have as many robots as smartphones.\n\n(Will write more on this…)");
            Assert.AreEqual(tweet.Id, 1633788842770825216);
            Assert.IsFalse(tweet.IsRetweet);
            Assert.IsTrue(tweet.IsReply);
        }
    }
}
