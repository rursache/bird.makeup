using System.Collections.Generic;
using BirdsiteLive.ActivityPub.Converters;
using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub.Models
{
    public class Featured
    {
        [JsonPropertyName("@context")]
        public string context { get; set; } = "https://www.w3.org/ns/activitystreams";

        public string id { get; set; }
        public string type { get; set; } = "OrderedCollection";
        public List<Note> orderedItems { get; set; } = new List<Note>();
    }
}