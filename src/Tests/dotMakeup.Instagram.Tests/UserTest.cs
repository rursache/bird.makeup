namespace dotMakeup.Instagram.Tests;

[TestClass]
public class UserTest
{
    [TestMethod]
    public async Task user_kobe()
    {
        var userService = new InstagramUserService();
        var user = await userService.GetUserAsync("kobebryant");
        Assert.AreEqual(user.Description, "Writer. Producer. Investor @granity @bryantstibel @drinkbodyarmor @mambamambacitasports");
        Assert.AreEqual(user.Name, "Kobe Bryant");
    }
}