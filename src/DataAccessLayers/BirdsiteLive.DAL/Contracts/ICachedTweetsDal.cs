using System.Threading.Tasks;
using BirdsiteLive.DAL.Models;

namespace BirdsiteLive.DAL.Contracts
{
    public interface ICachedTweetsDal
    {
        Task CreateTweetAsync(long tweetId, int userId, CachedTweet tweet);
        Task<CachedTweet> GetTweetAsync(long tweetId);
        Task DeleteTweetAsync(long tweetId);
    }
}