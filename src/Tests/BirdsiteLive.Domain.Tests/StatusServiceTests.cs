using System;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Domain.Statistics;
using BirdsiteLive.Domain.Tools;
using BirdsiteLive.Twitter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Domain.Tests
{
    [TestClass]
    public class StatusServiceTests
    {
        private readonly InstanceSettings _settings;

        #region Ctor
        public StatusServiceTests()
        {
            _settings = new InstanceSettings
            {
                Domain = "domain.name"
            };
        }
        #endregion

        [TestMethod]
        public void ActivityTest()
        {
            #region Stubs
            var username = "MyUserName";
            var extractedTweet = new ExtractedTweet
            {
                Id = 124L,
                CreatedAt = DateTime.UtcNow,
                MessageContent = @"Getting ready for the weekend...have a great one everyone!
⁠
Photo by Tim Tronckoe | @timtronckoe 
⁠
#archenemy #michaelamott #alissawhitegluz #jeffloomis #danielerlandsson #sharleedangelo⁠"
            };
            #endregion

            var logger1 = new Mock<ILogger<StatusExtractor>>();
            var statusExtractor = new StatusExtractor(_settings, logger1.Object);
            var stats = new Mock<IExtractionStatisticsHandler>();
            var service = new StatusService(_settings, statusExtractor, stats.Object);
            var result = service.GetActivity(username, extractedTweet);

            #region Validations

            #endregion
        }
    }
}
