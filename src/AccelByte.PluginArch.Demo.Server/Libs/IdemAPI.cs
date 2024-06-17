using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IdemUtils
{

    public static class IdemAPI
    {
        #region Collections

        [System.Serializable]
        public class AuthenticationResult
        {
            public string AccessToken;
            public int ExpiresIn;
            public string IdToken;
            public string RefreshToken;
            public string TokenType;
        }

        [System.Serializable]
        public class Root
        {
            public AuthenticationResult AuthenticationResult;
            public object ChallengeParameters;
        }

        [Serializable]
        public struct AddPlayerPayload
        {
            public string gameId;
            public string partyName;
            public List<Player> players;
        }

        [Serializable]
        public struct Player
        {
            public string playerId;
            public List<string> servers;
        }


        [Serializable]
        public struct GameIDPayload
        {
            public string gameId;
        }

        public struct MatchResponseData
        {
            public string action;
            public string messageId;
            public MatchPayload payload;
        }

        [System.Serializable]
        public struct MatchPayload
        {
            public string uid;
            public string gameId;
            public List<MatchData> matches;
        }

        [System.Serializable]
        public struct MatchData
        {
            public string uuid;
            public List<TeamData> teams;
        }

        [System.Serializable]
        public struct TeamData
        {
            public List<PlayerData> players;
        }

        [System.Serializable]
        public struct PlayerData
        {
            public string playerId;
            public string reference;
        }

        #endregion

        public static class URLS
        {
            public const string DOMAIN = "https://cognito-idp.eu-central-1.amazonaws.com/";
        }

        public static string GetIdemDestination(string token)
        {
            return $"wss://ws-int.idem.gg/?receiveMatches=true&gameMode=1v1&authorization={token}";
        }

        private static readonly HttpClient client = new HttpClient();

        public static async Task<AuthenticationResult> Authorize(string username, string password)
        {
            string report = string.Empty;
            var requestContent = $"{{\"AuthParameters\":" +
              $"{{\"USERNAME\": \"{username}\"," +
              $"\"PASSWORD\":\"{password}\"}}," +
              $"\"AuthFlow\":\"USER_PASSWORD_AUTH\"," +
              $"\"ClientId\":\"3b7bo4gjuqsjuer6eatjsgo58u\"}}";

            var content = new StringContent(requestContent, Encoding.UTF8, "application/x-amz-json-1.1");

            // Set the request URL
            var request = new HttpRequestMessage(HttpMethod.Post, URLS.DOMAIN)
            {
                Content = content
            };

            // Set the headers
            request.Headers.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

            try
            {
                // Send the request and get the response
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response
                report = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Root>(report).AuthenticationResult;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

        public static string BuildAction(string actionName, string payload)
        {
            return $@"{{
        ""action"": ""{actionName}"",
        ""payload"": {payload}
        }}";
        }


        public static async Task<string> AddPlayer(string Idtoken, AddPlayerPayload addPlayerRequest)
        {
            return await SendInternal(BuildAction("addPlayer", JsonConvert.SerializeObject(addPlayerRequest)), Idtoken);
        }

        public static async Task<MatchPayload> GetMatches(string Idtoken, GameIDPayload gameIDPayload)
        {
            var jsonData = await SendInternal(BuildAction("getMatches", JsonConvert.SerializeObject(gameIDPayload)), Idtoken);
            return JsonConvert.DeserializeObject<MatchResponseData>(jsonData).payload;
        }


        private static async Task<string> SendInternal(string messageToSend, string Idtoken)
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            var messageReceived = new StringBuilder();
            try
            {
                Uri serverUri = new Uri(GetIdemDestination(Idtoken));
                await webSocket.ConnectAsync(serverUri, CancellationToken.None);

                // Sending a message to the server
                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageToSend));
                await webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);

                // Receiving a message from the server
                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.EndOfMessage)
                {
                    messageReceived.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                messageReceived.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            catch (WebSocketException ex)
            {
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                webSocket.Dispose();
            }
            return messageReceived.ToString();
        }
    }

}

