using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
        private readonly ISaveProgressionTask _saveProgressionTask;
        private readonly ILogger<StatusPublicationPipeline> _logger;

        #region Ctor
        public StatusPublicationPipeline(IRetrieveTweetsProcessor retrieveTweetsProcessor, IRetrieveTwitterUsersProcessor retrieveTwitterAccountsProcessor, IRetrieveFollowersProcessor retrieveFollowersProcessor, ISendTweetsToFollowersProcessor sendTweetsToFollowersProcessor, ISaveProgressionTask saveProgressionTask, ILogger<StatusPublicationPipeline> logger)
        {
            _retrieveTweetsProcessor = retrieveTweetsProcessor;
            _retrieveFollowersProcessor = retrieveFollowersProcessor;
            _sendTweetsToFollowersProcessor = sendTweetsToFollowersProcessor;
            _saveProgressionTask = saveProgressionTask;
            _retrieveTwitterAccountsProcessor = retrieveTwitterAccountsProcessor;

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
            var retrieveTweetsBufferBlock = new BufferBlock<UserWithDataToSync[]>(new DataflowBlockOptions { BoundedCapacity = 20, CancellationToken = ct });
            var retrieveFollowersBlock = new TransformManyBlock<UserWithDataToSync[], UserWithDataToSync>(async x => await _retrieveFollowersProcessor.ProcessAsync(x, ct), standardBlockOptions);
            var retrieveFollowersBufferBlock = new BufferBlock<UserWithDataToSync>(new DataflowBlockOptions { BoundedCapacity = 500, CancellationToken = ct });
            var sendTweetsToFollowersBlock = new ActionBlock<UserWithDataToSync>(async x => await _sendTweetsToFollowersProcessor.ProcessAsync(x, ct), standardBlockOptions);

            // Link pipeline
            twitterUserToRefreshBufferBlock.LinkTo(retrieveTweetsBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveTweetsBlock.LinkTo(retrieveTweetsBufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveTweetsBufferBlock.LinkTo(retrieveFollowersBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveFollowersBlock.LinkTo(retrieveFollowersBufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveFollowersBufferBlock.LinkTo(sendTweetsToFollowersBlock, new DataflowLinkOptions { PropagateCompletion = true });

            // Launch twitter user retriever after a little delay
            // to give time for the Tweet cache to fill
            await Task.Delay(30 * 1000, ct);
            var retrieveTwitterAccountsTask = _retrieveTwitterAccountsProcessor.GetTwitterUsersAsync(twitterUserToRefreshBufferBlock, ct);

            // Wait
            await Task.WhenAny(new[] { retrieveTwitterAccountsTask, sendTweetsToFollowersBlock.Completion });

            var ex = retrieveTwitterAccountsTask.IsFaulted ? retrieveTwitterAccountsTask.Exception : sendTweetsToFollowersBlock.Completion.Exception;
            _logger.LogCritical(ex, "An error occurred, pipeline stopped");
        }
    }
}
