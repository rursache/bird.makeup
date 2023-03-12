using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub.Models
{
    public class ActivityDelete : Activity
    {
        [JsonPropertyName("object")]
        public string apObject { get; set; }
    }
}