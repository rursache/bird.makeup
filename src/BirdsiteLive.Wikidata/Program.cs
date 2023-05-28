using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

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
    Console.WriteLine(s[0]);
}

