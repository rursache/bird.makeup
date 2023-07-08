using dotMakeup.HackerNews;
using System.Threading.Tasks;
using Moq;

namespace dotMakeup.HackerNews.Tests;

[TestClass]
public class UsersTests
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(_ => _.CreateClient(string.Empty)).Returns(new HttpClient());
        var userService = new HNUserService(httpFactory.Object);
        var user = await userService.GetUserAsync("dhouston");
        
        Assert.AreEqual(user.About, "Founder/CEO of Dropbox (http://www.dropbox.com ; yc summer '07)");
    }
}