using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityAccept : Activity
    {
        [JsonPropertyName("object")]
        public NestedActivity apObject { get; set; }
    }
}