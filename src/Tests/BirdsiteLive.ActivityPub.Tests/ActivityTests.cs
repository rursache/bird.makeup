using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace BirdsiteLive.ActivityPub.Tests
{
    [TestClass]
    public class ActivityTests
    {
        [TestMethod]
        public void Serialize()
        {
            var obj = new Actor
            {
                type = "Person",
                id = "id"
            };

            var json = JsonConvert.SerializeObject(obj);


        }
    }
}
