using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityUndo : Activity
    {
        [JsonPropertyName("object")]
        public Activity apObject { get; set; }
    }
}