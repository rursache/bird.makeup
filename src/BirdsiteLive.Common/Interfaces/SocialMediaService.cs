using System.Threading.Tasks;

namespace BirdsiteLive.Common.Interfaces;

public interface ISocialMediaService
{
        Task<SocialMediaUser> GetUserAsync(string username);
}