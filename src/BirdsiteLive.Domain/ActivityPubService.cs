using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub;
using BirdsiteLive.ActivityPub.Converters;
using BirdsiteLive.ActivityPub.Models;
using BirdsiteLive.Common.Settings;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Domain
{
    public interface IActivityPubService
    {
        Task<Actor> GetUser(string objectId);
        Task<HttpStatusCode> PostDataAsync<T>(T data, string targetHost, string actorUrl, string inbox = null);
        Task PostNewActivity(ActivityCreateNote note, string username, string noteId, string targetHost,
            string targetInbox);

        ActivityAcceptFollow BuildAcceptFollow(ActivityFollow activity);
    }

    public class ActivityPubService : IActivityPubService
    {
        private readonly InstanceSettings _instanceSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<ActivityPubService> _logger;

        #region Ctor
        public ActivityPubService(ICryptoService cryptoService, InstanceSettings instanceSettings, IHttpClientFactory httpClientFactory, ILogger<ActivityPubService> logger)
        {
            _cryptoService = cryptoService;
            _instanceSettings = instanceSettings;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        #endregion

        public async Task<Actor> GetUser(string objectId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/activity+json");
            var result = await httpClient.GetAsync(objectId);

            if (result.StatusCode == HttpStatusCode.Gone)
                throw new FollowerIsGoneException();

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();

            var actor = JsonSerializer.Deserialize<Actor>(content);
            if (string.IsNullOrWhiteSpace(actor.url)) actor.url = objectId;
            return actor;
        }

        public async Task PostNewActivity(ActivityCreateNote noteActivity, string username, string noteId, string targetHost, string targetInbox)
        {
            try
            {
                var actor = UrlFactory.GetActorUrl(_instanceSettings.Domain, username);

                await PostDataAsync(noteActivity, targetHost, actor, targetInbox);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending {Username} post ({NoteId}) to {Host}{Inbox}", username, noteId, targetHost, targetInbox);
                throw;
            }
        }

        public ActivityAcceptFollow BuildAcceptFollow(ActivityFollow activity)
        {
            var acceptFollow = new ActivityAcceptFollow()
            {
                context = "https://www.w3.org/ns/activitystreams",
                id = $"{activity.apObject}#accepts/follows/{Guid.NewGuid()}",
                type = "Accept",
                actor = activity.apObject,
                apObject = new ActivityFollow()
                {
                    id = activity.id,
                    type = activity.type,
                    actor = activity.actor,
                    apObject = activity.apObject
                }
            };
            return acceptFollow;
        }
        public HttpRequestMessage BuildRequest<T>(T data, string targetHost, string actorUrl,
            string inbox = null)
        {
            var usedInbox = $"/inbox";
            if (!string.IsNullOrWhiteSpace(inbox))
                usedInbox = inbox;

            var json = JsonSerializer.Serialize(data);

            var date = DateTime.UtcNow.ToUniversalTime();
            var httpDate = date.ToString("r");

            var digest = _cryptoService.ComputeSha256Hash(json);

            var signature = _cryptoService.SignAndGetSignatureHeader(date, actorUrl, targetHost, digest, usedInbox);

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://{targetHost}{usedInbox}"),
                Headers =
                {
                    { "Host", targetHost },
                    { "Date", httpDate },
                    { "Signature", signature },
                    { "Digest", $"SHA-256={digest}" }
                },
                Content = new StringContent(json, Encoding.UTF8, "application/ld+json")
            };

            return httpRequestMessage;
        }

        public async Task<HttpStatusCode> PostDataAsync<T>(T data, string targetHost, string actorUrl, string inbox = null)
        {
            var httpRequestMessage = BuildRequest(data, targetHost, actorUrl, inbox);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            
            var response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Sent tweet to " + targetHost);

            return response.StatusCode;
        }
    }
}