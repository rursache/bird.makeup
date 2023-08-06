namespace dotMakeup.Instagram.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    [Ignore]
    public async Task user_grant()
    {
        var userService = new InstagramUser();
        var user = await userService.GetUserAsync("kobebryant");
        Assert.AreEqual(user, "Writer. Producer. Investor @granity @bryantstibel @drinkbodyarmor @mambamambacitasports");
    }
}