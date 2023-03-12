using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityRejectFollow : Activity
    {
        [JsonPropertyName("object")]
        public ActivityFollow apObject { get; set; }
    }
}