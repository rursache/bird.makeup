using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityFollow : Activity
    {
        [JsonPropertyName("object")]
        public string apObject { get; set; }
    }
}