using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class Activity
    {
        [JsonPropertyName("@context")]
        public string context { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string actor { get; set; }

        //[JsonPropertyName("object")]
        //public string apObject { get; set; }
    }
}