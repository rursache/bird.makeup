using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Moderation.Actions;
using BirdsiteLive.Pipeline.Models;
using BirdsiteLive.Pipeline.Processors;
using BirdsiteLive.Twitter;
using BirdsiteLive.Twitter.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Pipeline.Tests.Processors
{
    [TestClass]
    public class RefreshTwitterUserStatusProcessorTests
    {
        [TestMethod]
        public async Task ProcessAsync_Test()
        {
            #region Stubs
            var userId1 = 1;
            var userId2 = 2;

            var users = new List<SyncTwitterUser>
            {
                new SyncTwitterUser
                {
                    Id = userId1
                },
                new SyncTwitterUser
                {
                    Id = userId2
                }
            };

            var settings = new InstanceSettings
            {
                FailingTwitterUserCleanUpThreshold = 300
            };
            #endregion

            #region Mocks
            var twitterUserServiceMock = new Mock<ICachedTwitterUserService>(MockBehavior.Strict);
            twitterUserServiceMock
                .Setup(x => x.GetUserAsync(It.IsAny<string>()))
                .ReturnsAsync(new TwitterUser
                {
                    Protected = false
                });

            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            var removeTwitterAccountActionMock = new Mock<IRemoveTwitterAccountAction>(MockBehavior.Strict);
            #endregion

            var processor = new RefreshTwitterUserStatusProcessor(twitterUserServiceMock.Object, twitterUserDalMock.Object, removeTwitterAccountActionMock.Object, settings);
            var result = await processor.ProcessAsync(users.ToArray(), CancellationToken.None);

            #region Validations
            Assert.AreEqual(2 , result.Length);
            Assert.IsTrue(result.Any(x => x.User.Id == userId1));
            Assert.IsTrue(result.Any(x => x.User.Id == userId2));

            twitterUserDalMock.VerifyAll();
            removeTwitterAccountActionMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ProcessAsync_ResetErrorCount_Test()
        {
            #region Stubs
            var userId1 = 1;

            var users = new List<SyncTwitterUser>
            {
                new SyncTwitterUser
                {
                    Id = userId1,
                    FetchingErrorCount = 100
                }
            };

            var settings = new InstanceSettings
            {
                FailingTwitterUserCleanUpThreshold = 300
            };
            #endregion

            #region Mocks
            var twitterUserServiceMock = new Mock<ICachedTwitterUserService>(MockBehavior.Strict);
            twitterUserServiceMock
                .Setup(x => x.GetUserAsync(It.IsAny<string>()))
                .ReturnsAsync(new TwitterUser
                {
                    Protected = false
                });

            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            var removeTwitterAccountActionMock = new Mock<IRemoveTwitterAccountAction>(MockBehavior.Strict);
            #endregion

            var processor = new RefreshTwitterUserStatusProcessor(twitterUserServiceMock.Object, twitterUserDalMock.Object, removeTwitterAccountActionMock.Object, settings);
            var result = await processor.ProcessAsync(users.ToArray(), CancellationToken.None);

            #region Validations
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result.Any(x => x.User.Id == userId1));
            Assert.AreEqual(0, result.First().User.FetchingErrorCount);

            twitterUserDalMock.VerifyAll();
            removeTwitterAccountActionMock.VerifyAll();
            #endregion
        }



       
  

     
    } 
    
}