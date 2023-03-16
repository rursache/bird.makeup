using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityAcceptFollow : Activity
    {
        [JsonPropertyName("object")]
        public ActivityFollow apObject { get; set; }
    }
}