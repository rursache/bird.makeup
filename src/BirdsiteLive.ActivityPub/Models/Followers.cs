using BirdsiteLive.ActivityPub.Converters;
using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub.Models
{
    public class Followers
    {
        [JsonPropertyName("@context")]
        public string context { get; set; } = "https://www.w3.org/ns/activitystreams";

        public string id { get; set; }
        public string type { get; set; } = "OrderedCollection";
    }
}