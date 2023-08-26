﻿using System.Text.Json.Serialization;

namespace BirdsiteLive.ActivityPub.Models
{
    public class Note
    {
        [JsonPropertyName("@context")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] context { get; set; } = new[] { "https://www.w3.org/ns/activitystreams" };

        public string id { get; set; }
        public string announceId { get; set; }
        public string type { get; } = "Note";
        public string summary { get; set; }
        public string inReplyTo { get; set; }
        public string published { get; set; }
        public string url { get; set; }
        public string attributedTo { get; set; }
        public string[] to { get; set; }
        public string[] cc { get; set; }
        public bool sensitive { get; set; }
        //public string conversation { get; set; }
        public string content { get; set; }
        //public Dictionary<string,string> contentMap { get; set; }
        public Attachment[] attachment { get; set; }
        public Tag[] tag { get; set; }
        //public Dictionary<string, string> replies;
    }
}