using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityUndoFollow : Activity
    {
        [JsonPropertyName("object")]
        public ActivityFollow apObject { get; set; }
    }
}