using System.Text.Json;
using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub
{
    public class Attachment
    {
        public string type { get; set; }
        public string mediaType { get; set; }
        public string url { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string name { get; set; }
    }
}