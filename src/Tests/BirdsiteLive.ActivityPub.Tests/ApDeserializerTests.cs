using BirdsiteLive.ActivityPub.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace BirdsiteLive.ActivityPub.Tests
{
    [TestClass]
    public class ApDeserializerTests
    {
        [TestMethod]
        public void FollowDeserializationTest()
        {
            var json = "{ \"@context\":\"https://www.w3.org/ns/activitystreams\",\"id\":\"https://mastodon.technology/c94567cf-1fda-42ba-82fc-a0f82f63ccbe\",\"type\":\"Follow\",\"actor\":\"https://mastodon.technology/users/testtest\",\"object\":\"https://4a120ca2680e.ngrok.io/users/manu\"}";

            var data = ApDeserializer.ProcessActivity(json) as ActivityFollow;

            Assert.AreEqual("https://mastodon.technology/c94567cf-1fda-42ba-82fc-a0f82f63ccbe", data.id);
            Assert.AreEqual("Follow", data.type);
            Assert.AreEqual("https://4a120ca2680e.ngrok.io/users/manu", data.apObject);
        }

        [TestMethod]
        public void UndoDeserializationTest()
        {
            var json =
                "{\"@context\":\"https://www.w3.org/ns/activitystreams\",\"id\":\"https://mastodon.technology/users/testtest#follows/225982/undo\",\"type\":\"Undo\",\"actor\":\"https://mastodon.technology/users/testtest\",\"object\":{\"id\":\"https://mastodon.technology/c94567cf-1fda-42ba-82fc-a0f82f63ccbe\",\"type\":\"Follow\",\"actor\":\"https://mastodon.technology/users/testtest\",\"object\":\"https://4a120ca2680e.ngrok.io/users/manu\"}}";

            var data = ApDeserializer.ProcessActivity(json) as ActivityUndoFollow;
            Assert.AreEqual("https://mastodon.technology/users/testtest#follows/225982/undo", data.id);
            Assert.AreEqual("Undo", data.type);
            Assert.AreEqual("https://www.w3.org/ns/activitystreams", data.context);
            Assert.AreEqual("Follow", data.apObject.type);
            Assert.AreEqual("https://mastodon.technology/users/testtest", data.apObject.actor);
            Assert.AreEqual("https://4a120ca2680e.ngrok.io/users/manu", data.apObject.apObject);
            Assert.AreEqual(null, data.apObject.context);
        }

        [TestMethod]
        public void AcceptDeserializationTest()
        {
            var json = "{\"@context\":\"https://www.w3.org/ns/activitystreams\",\"id\":\"https://mamot.fr/users/testtest#accepts/follows/333879\",\"type\":\"Accept\",\"actor\":\"https://mamot.fr/users/testtest\",\"object\":{\"id\":\"https://85da1577f778.ngrok.io/f89dfd87-f5ce-4603-83d9-405c0e229989\",\"type\":\"Follow\",\"actor\":\"https://85da1577f778.ngrok.io/users/gra\",\"object\":\"https://mamot.fr/users/testtest\"}}";


            var data = ApDeserializer.ProcessActivity(json) as ActivityAcceptFollow;
            Assert.AreEqual("https://mamot.fr/users/testtest#accepts/follows/333879", data.id);
            Assert.AreEqual("Accept", data.type);
            Assert.AreEqual("https://mamot.fr/users/testtest", data.actor);
            Assert.AreEqual("https://85da1577f778.ngrok.io/f89dfd87-f5ce-4603-83d9-405c0e229989", data.apObject.id);
            Assert.AreEqual("https://85da1577f778.ngrok.io/users/gra", data.apObject.actor);
            Assert.AreEqual("Follow", data.apObject.type);
            Assert.AreEqual("https://mamot.fr/users/testtest", data.apObject.apObject);
        }

        [TestMethod]
        public void DeleteDeserializationTest()
        {
            var json =
                "{\"@context\": \"https://www.w3.org/ns/activitystreams\", \"id\": \"https://mastodon.technology/users/deleteduser#delete\", \"type\": \"Delete\", \"actor\": \"https://mastodon.technology/users/deleteduser\", \"to\": [\"https://www.w3.org/ns/activitystreams#Public\"],\"object\": \"https://mastodon.technology/users/deleteduser\",\"signature\": {\"type\": \"RsaSignature2017\",\"creator\": \"https://mastodon.technology/users/deleteduser#main-key\",\"created\": \"2020-11-19T22:43:01Z\",\"signatureValue\": \"peksQao4v5N+sMZgHXZ6xZnGaZrd0s+LqZimu63cnp7O5NBJM6gY9AAu/vKUgrh4C50r66f9OQdHg5yChQhc4ViE+yLR/3/e59YQimelmXJPpcC99Nt0YLU/iTRLsBehY3cDdC6+ogJKgpkToQvB6tG2KrPdrkreYh4Il4eXLKMfiQhgdKluOvenLnl2erPWfE02hIu/jpuljyxSuvJunMdU4yQVSZHTtk/I8q3jjzIzhgyb7ICWU5Hkx0H/47Q24ztsvOgiTWNgO+v6l9vA7qIhztENiRPhzGP5RCCzUKRAe6bcSu1Wfa3NKWqB9BeJ7s+2y2bD7ubPbiEE1MQV7Q==\"}}";

            var data = ApDeserializer.ProcessActivity(json) as ActivityDelete;

            Assert.AreEqual("https://mastodon.technology/users/deleteduser#delete", data.id);
            Assert.AreEqual("Delete", data.type);
            Assert.AreEqual("https://mastodon.technology/users/deleteduser", data.actor);
            Assert.AreEqual("https://mastodon.technology/users/deleteduser", data.apObject);
        }
        // {"object":{"object":"https://bird.makeup/users/spectatorindex","id":"https://masto.ai/b89eb86e-c902-48bc-956f-94f081617f18","type":"Follow","actor":"https://masto.ai/users/singha"},"@context":"https://www.w3.org/ns/activitystreams","id":"https://bird.makeup/users/spectatorindex#accepts/follows/27363118-e61e-4710-a41c-75dd5d54912f","type":"Accept","actor":"https://bird.makeup/users/spectatorindex"}
        // {"object":{"object":"https://bird.makeup/users/moltke","id":"https://universeodon.com/81cddd78-d7d6-4665-aa21-7bcfbea82b6b","type":"Follow","actor":"https://universeodon.com/users/amhrasmussen"},"@context":"https://www.w3.org/ns/activitystreams","id":"https://bird.makeup/users/moltke#accepts/follows/d28146be-e884-4e91-8385-19fa004f35b3","type":"Accept","actor":"https://bird.makeup/users/moltke"}


        [TestMethod]
        public void ActorDeserializationTest()
        {
            var json = """
					{
						"@context": [
							"https://www.w3.org/ns/activitystreams",
							"https://w3id.org/security/v1",
							{
								"manuallyApprovesFollowers": "as:manuallyApprovesFollowers",
								"toot": "http://joinmastodon.org/ns#",
								"featured": {
									"@id": "toot:featured",
									"@type": "@id"
								},
								"featuredTags": {
									"@id": "toot:featuredTags",
									"@type": "@id"
								},
								"alsoKnownAs": {
									"@id": "as:alsoKnownAs",
									"@type": "@id"
								},
								"movedTo": {
									"@id": "as:movedTo",
									"@type": "@id"
								},
								"schema": "http://schema.org#",
								"PropertyValue": "schema:PropertyValue",
								"value": "schema:value",
								"discoverable": "toot:discoverable",
								"Device": "toot:Device",
								"Ed25519Signature": "toot:Ed25519Signature",
								"Ed25519Key": "toot:Ed25519Key",
								"Curve25519Key": "toot:Curve25519Key",
								"EncryptedMessage": "toot:EncryptedMessage",
								"publicKeyBase64": "toot:publicKeyBase64",
								"deviceId": "toot:deviceId",
								"claim": {
									"@type": "@id",
									"@id": "toot:claim"
								},
								"fingerprintKey": {
									"@type": "@id",
									"@id": "toot:fingerprintKey"
								},
								"identityKey": {
									"@type": "@id",
									"@id": "toot:identityKey"
								},
								"devices": {
									"@type": "@id",
									"@id": "toot:devices"
								},
								"messageFranking": "toot:messageFranking",
								"messageType": "toot:messageType",
								"cipherText": "toot:cipherText",
								"suspended": "toot:suspended"
							}
						],
						"id": "https://mastodon.online/users/devvincent",
						"type": "Person",
						"following": "https://mastodon.online/users/devvincent/following",
						"followers": "https://mastodon.online/users/devvincent/followers",
						"inbox": "https://mastodon.online/users/devvincent/inbox",
						"outbox": "https://mastodon.online/users/devvincent/outbox",
						"featured": "https://mastodon.online/users/devvincent/collections/featured",
						"featuredTags": "https://mastodon.online/users/devvincent/collections/tags",
						"preferredUsername": "devvincent",
						"name": "",
						"summary": "",
						"url": "https://mastodon.online/@devvincent",
						"manuallyApprovesFollowers": false,
						"discoverable": false,
						"published": "2022-05-08T00:00:00Z",
						"devices": "https://mastodon.online/users/devvincent/collections/devices",
						"publicKey": {
							"id": "https://mastodon.online/users/devvincent#main-key",
							"owner": "https://mastodon.online/users/devvincent",
							"publicKeyPem": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA7U07uS4zu5jeZSBVZ072\naXcTeVQc0baM8BBUvJkpX+mV2vh+V4yfqN44KzFxlkk8XcAoidt8HBAvpQ/5yCwZ\neGS2ySCxC+sqvErIbaYadWVHGJhZjLYPVa0n8wvkqRQ0aUJ8K17/wY+/YYfukgeC\nTGHGoyzDDZZxrR1Z8LTvImSEkYooTvvzaaFaTUnFwCKepxftKLdJAfp4sP4l1Zom\nUZGwaYimuJmN1bfhet/2v0S7M7/XPlmVRpfUluE2vYE0RtJt3BVDZfoWEGJPk9us\nN/JHu6UBUh6UM6ASFy5MlDLh36OxyO9sVx1WgQlNDmu2qcGUIkIgqTKppDCIP3Xk\nVQIDAQAB\n-----END PUBLIC KEY-----\n"
						},
						"tag": [],
						"attachment": [],
						"endpoints": {
							"sharedInbox": "https://mastodon.online/inbox"
						}
					}
				""";

            var actor = JsonSerializer.Deserialize<Actor>(json);
        }
    }
}