using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BirdsiteLive.Pipeline.Models;

namespace BirdsiteLive.Pipeline.Contracts
{
    public interface IRetrieveTwitterUsersProcessor
    {
        Task GetTwitterUsersAsync(BufferBlock<UserWithDataToSync[]> twitterUsersBufferBlock, CancellationToken ct);
    }
}