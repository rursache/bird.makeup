using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.DAL.Postgres.DataAccessLayers;
using BirdsiteLive.DAL.Postgres.Settings;

var settings = new PostgresSettings()
{
    ConnString = System.Environment.GetEnvironmentVariable("ConnString"),
};
var dal = new TwitterUserPostgresDal(settings);

var twitterUser = new HashSet<string>();
var twitterUserQuery = await dal.GetAllTwitterUsersAsync();
Console.WriteLine("Loading twitter users");
foreach (SyncTwitterUser user in twitterUserQuery)
{
    twitterUser.Add(user.Acct);
}
Console.WriteLine("Done loading twitter users");

Console.WriteLine("Hello, World!");
var client = new HttpClient();
string query = new StreamReader("query.sparql").ReadToEnd();

client.DefaultRequestHeaders.Add("Accept", "text/csv");
client.DefaultRequestHeaders.Add("User-Agent", "BirdMakeup/1.0 (https://bird.makeup; coolbot@example.org) BirdMakeup/1.0");
var response = await client.GetAsync($"https://query.wikidata.org/sparql?query={Uri.EscapeDataString(query)}");
var content = await response.Content.ReadAsStringAsync();

// Console.WriteLine(content);

foreach (string n in content.Split("\n"))
{
    var s = n.Split(",");
    if (n.Length < 2)
        continue;
    
    var acct = s[1];
    var fedi = s[2];
    await dal.UpdateTwitterUserFediAcctAsync(acct, fedi);
    if (twitterUser.Contains(acct))
        Console.WriteLine(fedi);
}

