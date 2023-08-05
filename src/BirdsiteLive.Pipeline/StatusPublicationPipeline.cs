using System;
using System.ComponentModel.Design.Serialization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Pipeline.Contracts;
using BirdsiteLive.Pipeline.Models;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Pipeline
{
    public interface IStatusPublicationPipeline
    {
        Task ExecuteAsync(CancellationToken ct);
    }

    public class StatusPublicationPipeline : IStatusPublicationPipeline
    {
        private readonly IRetrieveTwitterUsersProcessor _retrieveTwitterAccountsProcessor;
        private readonly IRetrieveTweetsProcessor _retrieveTweetsProcessor;
        private readonly IRetrieveFollowersProcessor _retrieveFollowersProcessor;
        private readonly ISendTweetsToFollowersProcessor _sendTweetsToFollowersProcessor;
        private readonly InstanceSettings _instanceSettings;
        private readonly ILogger<StatusPublicationPipeline> _logger;

        #region Ctor
        public StatusPublicationPipeline(IRetrieveTweetsProcessor retrieveTweetsProcessor, IRetrieveTwitterUsersProcessor retrieveTwitterAccountsProcessor, IRetrieveFollowersProcessor retrieveFollowersProcessor, ISendTweetsToFollowersProcessor sendTweetsToFollowersProcessor, InstanceSettings instanceSettings, ILogger<StatusPublicationPipeline> logger)
        {
            _retrieveTweetsProcessor = retrieveTweetsProcessor;
            _retrieveFollowersProcessor = retrieveFollowersProcessor;
            _sendTweetsToFollowersProcessor = sendTweetsToFollowersProcessor;
            _retrieveTwitterAccountsProcessor = retrieveTwitterAccountsProcessor;
            _instanceSettings = instanceSettings;

            _logger = logger;
        }
        #endregion

        public async Task ExecuteAsync(CancellationToken ct)
        {
            var standardBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, MaxDegreeOfParallelism = 1, CancellationToken = ct};
            // Create blocks 
            var twitterUserToRefreshBufferBlock = new BufferBlock<UserWithDataToSync[]>(new DataflowBlockOptions
                { BoundedCapacity = 1, CancellationToken = ct });
            var retrieveTweetsBlock = new TransformBlock<UserWithDataToSync[], UserWithDataToSync[]>(async x =>  await _retrieveTweetsProcessor.ProcessAsync(x, ct), standardBlockOptions );
            var retrieveTweetsBufferBlock = new BufferBlock<UserWithDataToSync[]>(new DataflowBlockOptions { BoundedCapacity = 2, CancellationToken = ct });
           // var retrieveFollowersBlock = new TransformManyBlock<UserWithDataToSync[], UserWithDataToSync>(async x => await _retrieveFollowersProcessor.ProcessAsync(x, ct), new ExecutionDataflowBlockOptions { BoundedCapacity = 1 } );
           // var retrieveFollowersBufferBlock = new BufferBlock<UserWithDataToSync>(new DataflowBlockOptions { BoundedCapacity = 500, CancellationToken = ct });
            var sendTweetsToFollowersBlock = new ActionBlock<UserWithDataToSync[]>(async x => await _sendTweetsToFollowersProcessor.ProcessAsync(x, ct), standardBlockOptions);

            // Link pipeline
            twitterUserToRefreshBufferBlock.LinkTo(retrieveTweetsBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveTweetsBlock.LinkTo(retrieveTweetsBufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveTweetsBufferBlock.LinkTo(sendTweetsToFollowersBlock, new DataflowLinkOptions { PropagateCompletion = true });

            // Launch twitter user retriever after a little delay
            // to give time for the Tweet cache to fill
            await Task.Delay(_instanceSettings.PipelineStartupDelay * 1000, ct);
            var retrieveTwitterAccountsTask = _retrieveTwitterAccountsProcessor.GetTwitterUsersAsync(twitterUserToRefreshBufferBlock, ct);

            // Wait
            await Task.WhenAny(new[] { retrieveTwitterAccountsTask, sendTweetsToFollowersBlock.Completion });

            var ex = retrieveTwitterAccountsTask.IsFaulted ? retrieveTwitterAccountsTask.Exception : sendTweetsToFollowersBlock.Completion.Exception;
            _logger.LogCritical(ex, "An error occurred, pipeline stopped");
        }
    }
}
