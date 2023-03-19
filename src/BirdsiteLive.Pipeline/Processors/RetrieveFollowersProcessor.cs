using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.Pipeline.Contracts;
using BirdsiteLive.Pipeline.Models;

namespace BirdsiteLive.Pipeline.Processors
{
    public class RetrieveFollowersProcessor : IRetrieveFollowersProcessor
    {
        private readonly IFollowersDal _followersDal;

        #region Ctor
        public RetrieveFollowersProcessor(IFollowersDal followersDal)
        {
            _followersDal = followersDal;
        }
        #endregion

        public async Task<IEnumerable<UserWithDataToSync>> ProcessAsync(UserWithDataToSync[] userWithTweetsToSyncs, CancellationToken ct)
        {
            List<Task> todo = new List<Task>();
            foreach (var user in userWithTweetsToSyncs)
            {
                var t = Task.Run( 
                async() => {
                    var followers = await _followersDal.GetFollowersAsync(user.User.Id);
                    user.Followers = followers;
                });
                todo.Add(t);
            }
            
            await Task.WhenAll(todo);

            return userWithTweetsToSyncs;
        }
    }
}