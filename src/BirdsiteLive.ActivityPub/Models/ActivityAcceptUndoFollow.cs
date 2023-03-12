using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityAcceptUndoFollow : Activity
    {
        [JsonPropertyName("object")]
        public ActivityUndoFollow apObject { get; set; }
    }
}