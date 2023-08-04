using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.Pipeline.Models;
using BirdsiteLive.Pipeline.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Pipeline.Tests
{
    [TestClass]
    public class StatusPublicationPipelineTests
    {
        [TestMethod]
        public async Task ExecuteAsync_Test()
        {
            #region Stubs
            var ct = new CancellationTokenSource(100 * 1000);
            #endregion

            #region Mocks

            var retrieveTwitterUserProcessor = new Mock<IRetrieveTwitterUsersProcessor>(MockBehavior.Strict);
            retrieveTwitterUserProcessor
                .Setup(x => x.GetTwitterUsersAsync(
                    It.IsAny<BufferBlock<UserWithDataToSync[]>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(0));
            var retrieveTweetsProcessor = new Mock<IRetrieveTweetsProcessor>(MockBehavior.Strict);
            var retrieveFollowersProcessor = new Mock<IRetrieveFollowersProcessor>(MockBehavior.Strict);
            var sendTweetsToFollowersProcessor = new Mock<ISendTweetsToFollowersProcessor>(MockBehavior.Strict);
            var logger = new Mock<ILogger<StatusPublicationPipeline>>();

            var setting = new InstanceSettings()
            {
                PipelineStartupDelay = 1
            };
            #endregion

            var pipeline = new StatusPublicationPipeline(retrieveTweetsProcessor.Object, retrieveTwitterUserProcessor.Object, retrieveFollowersProcessor.Object, sendTweetsToFollowersProcessor.Object, setting, logger.Object);
            await pipeline.ExecuteAsync(ct.Token);

            #region Validations
            retrieveTweetsProcessor.VerifyAll();
            retrieveFollowersProcessor.VerifyAll();
            sendTweetsToFollowersProcessor.VerifyAll();
            logger.VerifyAll();
            #endregion
        }
    }
}