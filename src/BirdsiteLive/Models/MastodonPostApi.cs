using System;

namespace BirdsiteLive.Models;

public class MastodonPostApi
{
    public long id { get; set; }
    public string created_at { get; set; }
    public long? in_reply_to_id { get; set; } = null;
    public long? in_reply_to_account_id { get; set; } = null;
    public bool sensitive { get; set; } = false;
    public string spoiler_text { get; set; } = "";
    public string visibility { get; set; } = "public";
    public string language { get; set; } = "en";
    public string uri { get; set; } 
    public string url { get; set; } 
    public int replies_count { get; set; } = 0;
    public int reblogs_count { get; set; } = 0;
    public int favorite_count { get; set; } = 0;
    public string content { get; set; }
    public MastodonUserApi account { get; set; }
    public MastodonAppApi application { get; } = new MastodonAppApi();
}
public class MastodonUserApi
{
    public long Id { get; set; }
    public string username { get; set; }
    public string acct { get; set; }
    public string display_name { get; set; }
    public bool locked { get; set; } = false;
    public bool bot { get; set; } = true;
    public bool group { get; set; } = false;
    public string Note { get; set; }
    public string Url { get; set; }
    public string avatar { get; set; }
    public string avatar_static { get; set; }
    public string header { get; set; }
    public string header_static { get; set; }
    public int followers_count { get; set; } = 0;
    public int following_count { get; set; } = 0;
    public int statuses_count { get; set; } = 0;
}

public class MastodonAppApi
{
    public string name { get; set; } = "bird.makeup";
    public string url { get; set; } = "https://bird.makeup/";
}
