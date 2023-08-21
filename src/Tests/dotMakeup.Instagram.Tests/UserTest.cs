using dotMakeup.Instagram.Models;

namespace dotMakeup.Instagram.Tests;

[TestClass]
public class UserTest
{
    [TestMethod]
    public async Task user_kobe()
    {
        var userService = new InstagramUserService();
        InstagramUser user;
        try
        {
            user = await userService.GetUserAsync("kobebryant");
        }
        catch (Exception _)
        {
            Assert.Inconclusive();
            return;
        }
        Assert.AreEqual(user.Description, "Writer. Producer. Investor @granity @bryantstibel @drinkbodyarmor @mambamambacitasports");
        Assert.AreEqual(user.Name, "Kobe Bryant");
    }
}