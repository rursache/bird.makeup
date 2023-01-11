﻿using System;
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
        private readonly ISaveProgressionProcessor _saveProgressionProcessor;
        private readonly ILogger<StatusPublicationPipeline> _logger;

        #region Ctor
        public StatusPublicationPipeline(IRetrieveTweetsProcessor retrieveTweetsProcessor, IRetrieveTwitterUsersProcessor retrieveTwitterAccountsProcessor, IRetrieveFollowersProcessor retrieveFollowersProcessor, ISendTweetsToFollowersProcessor sendTweetsToFollowersProcessor, ISaveProgressionProcessor saveProgressionProcessor, ILogger<StatusPublicationPipeline> logger)
        {
            _retrieveTweetsProcessor = retrieveTweetsProcessor;
            _retrieveFollowersProcessor = retrieveFollowersProcessor;
            _sendTweetsToFollowersProcessor = sendTweetsToFollowersProcessor;
            _saveProgressionProcessor = saveProgressionProcessor;
            _retrieveTwitterAccountsProcessor = retrieveTwitterAccountsProcessor;

            _logger = logger;
        }
        #endregion

        public async Task ExecuteAsync(CancellationToken ct)
        {
            var standardBlockOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };
            // Create blocks 
            var twitterUserToRefreshBufferBlock = new BufferBlock<UserWithDataToSync[]>(new DataflowBlockOptions
                { BoundedCapacity = 1, CancellationToken = ct });
            var retrieveTweetsBlock = new TransformBlock<UserWithDataToSync[], UserWithDataToSync[]>(async x =>  await _retrieveTweetsProcessor.ProcessAsync(x, ct), new ExecutionDataflowBlockOptions { BoundedCapacity = 1, MaxDegreeOfParallelism = 2 } );
            var retrieveTweetsBufferBlock = new BufferBlock<UserWithDataToSync[]>(new DataflowBlockOptions { BoundedCapacity = 20, CancellationToken = ct });
            var retrieveFollowersBlock = new TransformManyBlock<UserWithDataToSync[], UserWithDataToSync>(async x => await _retrieveFollowersProcessor.ProcessAsync(x, ct), standardBlockOptions);
            var retrieveFollowersBufferBlock = new BufferBlock<UserWithDataToSync>(new DataflowBlockOptions { BoundedCapacity = 20, CancellationToken = ct });
            var sendTweetsToFollowersBlock = new TransformBlock<UserWithDataToSync, UserWithDataToSync>(async x => await _sendTweetsToFollowersProcessor.ProcessAsync(x, ct), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct, BoundedCapacity = 1 });
            var sendTweetsToFollowersBufferBlock = new BufferBlock<UserWithDataToSync>(new DataflowBlockOptions { BoundedCapacity = 20, CancellationToken = ct });
            var saveProgressionBlock = new ActionBlock<UserWithDataToSync>(async x => await _saveProgressionProcessor.ProcessAsync(x, ct), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct, BoundedCapacity = 1 });

            // Link pipeline
            twitterUserToRefreshBufferBlock.LinkTo(retrieveTweetsBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveTweetsBlock.LinkTo(retrieveTweetsBufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveTweetsBufferBlock.LinkTo(retrieveFollowersBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveFollowersBlock.LinkTo(retrieveFollowersBufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            retrieveFollowersBufferBlock.LinkTo(sendTweetsToFollowersBlock, new DataflowLinkOptions { PropagateCompletion = true });
            sendTweetsToFollowersBlock.LinkTo(sendTweetsToFollowersBufferBlock, new DataflowLinkOptions { PropagateCompletion = true });
            sendTweetsToFollowersBufferBlock.LinkTo(saveProgressionBlock, new DataflowLinkOptions { PropagateCompletion = true });

            // Launch twitter user retriever
            var retrieveTwitterAccountsTask = _retrieveTwitterAccountsProcessor.GetTwitterUsersAsync(twitterUserToRefreshBufferBlock, ct);

            // Wait
            await Task.WhenAny(new[] { retrieveTwitterAccountsTask, saveProgressionBlock.Completion });

            var ex = retrieveTwitterAccountsTask.IsFaulted ? retrieveTwitterAccountsTask.Exception : saveProgressionBlock.Completion.Exception;
            _logger.LogCritical(ex, "An error occurred, pipeline stopped");
        }
    }
}
