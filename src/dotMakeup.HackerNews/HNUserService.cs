using System.Text.Json;
using System.Web;
using dotMakeup.HackerNews.Models;

namespace dotMakeup.HackerNews;

public class HNUserService
{
    private IHttpClientFactory _httpClientFactory;
    public HNUserService(IHttpClientFactory httpClientFactory)
    {
            _httpClientFactory = httpClientFactory;
        
    }
    public async Task<HNUser> GetUserAsync(string username)
    {
        string reqURL = "https://hacker-news.firebaseio.com/v0/user/dhouston.json";
        reqURL = reqURL.Replace("dhouston", username);
        
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod("GET"), reqURL);
        
        JsonDocument userDoc;
        var httpResponse = await client.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();
        var c = await httpResponse.Content.ReadAsStringAsync();
        userDoc = JsonDocument.Parse(c);

        string about =
            HttpUtility.HtmlDecode(userDoc.RootElement.GetProperty("about").GetString());
        
        var user = new HNUser()
        {
            Id = 0,
            About = about,
        };
        return user;
    }
}