using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Pipeline.Processors;
using BirdsiteLive.Pipeline.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Pipeline.Tests.Processors
{
    [TestClass]
    public class RetrieveTwitterUsersProcessorTests
    {
        [TestMethod]
        public async Task GetTwitterUsersAsync_Test()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();
            var users = new[]
            {
                new SyncTwitterUser(),
                new SyncTwitterUser(),
                new SyncTwitterUser(),
            };
            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 1,
            };
            #endregion

            #region Mocks
            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .Setup(x => x.GetAllTwitterUsersWithFollowersAsync(
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true)))
                .ReturnsAsync(users);
            
            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            var t = processor.GetTwitterUsersAsync(buffer, CancellationToken.None);

            await Task.WhenAny(t, Task.Delay(50));

            #region Validations
            twitterUserDalMock.VerifyAll();
            Assert.IsTrue(0 < buffer.Count);
            buffer.TryReceive(out var result);
            Assert.IsTrue(0 < result.Length);
            #endregion
        }

        [TestMethod]
        public async Task GetTwitterUsersAsync_Multi_Test()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();
            var users = new List<SyncTwitterUser>();

            for (var i = 0; i < 30; i++)
                users.Add(new SyncTwitterUser());

            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 1,
            };
            #endregion

            #region Mocks
            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .SetupSequence(x => x.GetAllTwitterUsersWithFollowersAsync(
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true)))
                .ReturnsAsync(users.ToArray())
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0]);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            var t = processor.GetTwitterUsersAsync(buffer, CancellationToken.None);

            await Task.WhenAny(t, Task.Delay(300));
            
            #region Validations
            twitterUserDalMock.VerifyAll();
            Assert.IsTrue(0 < buffer.Count);
            buffer.TryReceive(out var result);
            Assert.IsTrue(1 < result.Length);
            #endregion
        }

        [TestMethod]
        public async Task GetTwitterUsersAsync_Sharding()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();
            var users = new List<SyncTwitterUser>();

            for (var i = 0; i < 200; i++)
                users.Add(new SyncTwitterUser());

            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 0,
                n_end = 10,
                m = 100,
                MultiplyNByOrdinal = true,
                MachineName = "dotmakeup-3"
            };
            #endregion

            #region Mocks
            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .SetupSequence(x => x.GetAllTwitterUsersWithFollowersAsync(
                    It.Is<int>(y => true),
                    It.Is<int>(y => y == 30),
                    It.Is<int>(y => y == 40),
                    It.Is<int>(y => true)))
                .ReturnsAsync(users.ToArray())
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0]);
            
            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            var t = processor.GetTwitterUsersAsync(buffer, CancellationToken.None);

            await Task.WhenAny(t, Task.Delay(5000));

            #region Validations
            twitterUserDalMock.VerifyAll();
            Assert.IsTrue(0 < buffer.Count);
            buffer.TryReceive(out var result);
            Assert.IsTrue(1 < result.Length);
            #endregion
        }
        [TestMethod]
        public async Task GetTwitterUsersAsync_Multi2_Test()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();
            var users = new List<SyncTwitterUser>();

            for (var i = 0; i < 31; i++)
                users.Add(new SyncTwitterUser());

            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 1,
            };
            #endregion

            #region Mocks
            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .SetupSequence(x => x.GetAllTwitterUsersWithFollowersAsync(
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true)))
                .ReturnsAsync(users.ToArray())
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0])
                .ReturnsAsync(new SyncTwitterUser[0]);
            
            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            var t = processor.GetTwitterUsersAsync(buffer, CancellationToken.None);

            await Task.WhenAny(t, Task.Delay(5000));

            #region Validations
            twitterUserDalMock.VerifyAll();
            Assert.IsTrue(0 < buffer.Count);
            buffer.TryReceive(out var result);
            Assert.IsTrue(1 < result.Length);
            #endregion
        }

        [TestMethod]
        public async Task GetTwitterUsersAsync_NoUsers_Test()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();

            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 1,
            };
            #endregion

            #region Mocks

            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .Setup(x => x.GetAllTwitterUsersWithFollowersAsync(
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true)))
                .ReturnsAsync(new SyncTwitterUser[0]);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            var t =processor.GetTwitterUsersAsync(buffer, CancellationToken.None);

            await Task.WhenAny(t, Task.Delay(50));

            #region Validations
            twitterUserDalMock.VerifyAll();
            Assert.AreEqual(0, buffer.Count);
            #endregion
        }
        
        [TestMethod]
        public async Task GetTwitterUsersAsync_Exception_Test()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();

            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 1,
            };
            #endregion

            #region Mocks
            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            twitterUserDalMock
                .Setup(x => x.GetAllTwitterUsersWithFollowersAsync(
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true),
                    It.Is<int>(y => true)))
                .Returns(async () => await DelayFaultedTask<SyncTwitterUser[]>(new Exception()));

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            var t = processor.GetTwitterUsersAsync(buffer, CancellationToken.None);

            await Task.WhenAny(t, Task.Delay(50));

            #region Validations
            twitterUserDalMock.VerifyAll();
            Assert.AreEqual(0, buffer.Count);
            #endregion
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task GetTwitterUsersAsync_Cancellation_Test()
        {
            #region Stubs
            var buffer = new BufferBlock<UserWithDataToSync[]>();
            var canTokenS = new CancellationTokenSource();
            canTokenS.Cancel();

            var maxUsers = 1000;
            var instanceSettings = new InstanceSettings()
            {
                n_start = 1,
            };
            #endregion

            #region Mocks
            var twitterUserDalMock = new Mock<ITwitterUserDal>(MockBehavior.Strict);
            
            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.GetFollowersAsync(It.Is<int>(x => true)))
                .ReturnsAsync(new Follower[] {});
            
            var loggerMock = new Mock<ILogger<RetrieveTwitterUsersProcessor>>();
            #endregion

            var processor = new RetrieveTwitterUsersProcessor(twitterUserDalMock.Object, followersDalMock.Object, instanceSettings, loggerMock.Object);
            await processor.GetTwitterUsersAsync(buffer, canTokenS.Token);
        }

        private static async Task<T> DelayFaultedTask<T>(Exception e)
        {
            await Task.Delay(30);
            throw e;
        }
    }
}
