using BirdsiteLive.ActivityPub.Models;
using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class ActivityCreateNote : Activity
    {
        public string published { get; set; }
        public string[] to { get; set; }
        public string[] cc { get; set; }

        [JsonPropertyName("object")]
        public Note apObject { get; set; }
    }
}