using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityAcceptFollow : Activity
    {
        [JsonPropertyName("object")]
        public NestedActivity apObject { get; set; }
    }
}