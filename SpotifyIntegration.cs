using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;

namespace MyCoolMod
{
    public class SpotifyIntegration
    {
        private string clientId;
        private string clientSecret;
        private string redirectUri = "http://localhost:5000/callback";
        private string accessToken;
        private string refreshToken;
        private TrackFetcher trackFetcher;

        private DateTime accessTokenExpirationTime;

        public SpotifyIntegration(string clientId, string clientSecret, string githubRawUrl)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            trackFetcher = new TrackFetcher(githubRawUrl);
        }

        public async Task StartAuthProcess()
        {
            try
            {
                string authorizationEndpoint = "https://accounts.spotify.com/authorize";
                string responseType = "code";
                string scope = "user-read-currently-playing user-read-playback-state user-modify-playback-state";

                string authorizationUrl = $"{authorizationEndpoint}?client_id={clientId}&response_type={responseType}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scope)}";

                // Open the authorization URL in the user's browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                });

                // Start a local HTTP server to listen for the callback
                HttpListener httpListener = new HttpListener();
                httpListener.Prefixes.Add(redirectUri + "/");
                httpListener.Start();
                HttpListenerContext context = await httpListener.GetContextAsync();

                // Extract the authorization code from the query param
                string authorizationCode = context.Request.QueryString["code"];

                // Respond to the client
                using (var response = context.Response)
                {
                    string responseString = "Authorization code received. You can close this window.";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                // Exchange the auth code for an access token
                await ExchangeAuthorizationCodeForTokens(authorizationCode);

                httpListener.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start Spotify auth process: {ex.Message}");
            }
        }

        private async Task ExchangeAuthorizationCodeForTokens(string authorizationCode)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                });

                request.Content = requestBody;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenInfo = JObject.Parse(responseBody);
                accessToken = tokenInfo["access_token"].ToString();
                refreshToken = tokenInfo["refresh_token"].ToString();

                int expiresIn = tokenInfo["expires_in"].Value<int>();
                accessTokenExpirationTime = DateTime.Now.AddSeconds(expiresIn);

                Debug.Log("Access Token: REDACTED(##########)");
                Debug.Log("Refresh Token: REDACTED(##########)");
                Debug.Log(accessTokenExpirationTime.ToString());
            }
        }


        public async Task<string> GetCurrentlyPlayingAsync()
        {
            await EnsureAccessTokenValid();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var trackInfo = JObject.Parse(responseBody);
                    if (trackInfo["item"] != null)
                    {
                        var trackName = trackInfo["item"]["name"].ToString();
                        var artistName = trackInfo["item"]["artists"][0]["name"].ToString();
                        return $"{trackName} by {artistName}";
                    }
                    else
                    {
                        return "No song is currently playing.";
                    }
                }
                else
                {
                    return $"Error fetching currently playing song: {response.ReasonPhrase}";
                }
            }
        }

        public async Task SkipSongAsync()
        {
            await EnsureAccessTokenValid();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.PostAsync("https://api.spotify.com/v1/me/player/next", null);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Successfully skipped the song.");
                }
                else
                {
                    Debug.LogError($"Failed to skip the song: {response.ReasonPhrase}");
                }
            }
        }

        public async Task PreviousSongAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.PostAsync("https://api.spotify.com/v1/me/player/previous", null);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Successfully skipped to previous song");
                }
                else
                {
                    Debug.LogError($"Failed to skip the song: {response.ReasonPhrase}");
                }
            }
        }
        public async Task StartSongAsync()
        {
            await EnsureAccessTokenValid();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Spotify API requires a PUT request to start playback
                var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/play");

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Successfully Started song");
                }
                else
                {
                    Debug.LogError($"Failed to start the Song: {response.ReasonPhrase}");
                }
            }
        }

        public async Task PauseSongAsync()
        {
            await EnsureAccessTokenValid();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Spotify API requires a PUT request to pause playback
                var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/pause");

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("Successfully Paused song");
                }
                else
                {
                    Debug.LogError($"Failed to Pause the song: {response.ReasonPhrase}");
                }
            }
        }

        public async Task RandomSongAsync()
        {
            await EnsureAccessTokenValid();
            try
            {
                List<string> trackIds = await trackFetcher.FetchTrackIdsAsync();

                if (trackIds == null || trackIds.Count == 0)
                {
                    Debug.LogError("No track IDs found.");
                    return;
                }

                var random = new System.Random();
                var randomIndex = random.Next(0, trackIds.Count);
                var randomSongId = trackIds[randomIndex];

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player/play");

                    var requestBody = new JObject
                    {
                        ["uris"] = new JArray($"spotify:track:{randomSongId}")
                    };

                    request.Content = new StringContent(requestBody.ToString(), System.Text.Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log("Successfully started a random song");
                    }
                    else
                    {
                        Debug.LogError($"Failed to start a random song: {response.ReasonPhrase}");
                    }
                }
            } catch (Exception ex)
            {
                Debug.LogError($"Failed to start random song: {ex.Message}");
            }
            
        }
        public async Task IncreaseVolumeAsync()
        {
            await EnsureAccessTokenValid();
            try
            {
                const int volumeIncreasePercentage = 10; // Increase volume by 10%
                const int maxVolume = 100;

                int currentVolume = await GetCurrentVolumeAsync();
                int newVolume = currentVolume + volumeIncreasePercentage;

                if (newVolume > maxVolume)
                {
                    newVolume = maxVolume;
                }

                await SetVolumeAsync(newVolume);
            } catch (Exception ex)
            {
                Debug.LogError($"Failed to Increase volume: {ex.Message}");
            }
            
        }
        public async Task DecreaseVolumeAsync()
        {
            await EnsureAccessTokenValid();
            try
            {
                var currentVolume = await GetCurrentVolumeAsync();
                int decreasedVolume = (int)Math.Max(currentVolume - 10, 0); // Ensure volume doesn't go below 0
                await SetVolumeAsync(decreasedVolume);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to decrease volume: {ex.Message}");
            }
        }



        public async Task<int> GetCurrentVolumeAsync()
        {
            await EnsureAccessTokenValid();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync("https://api.spotify.com/v1/me/player");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var playerInfo = JObject.Parse(responseBody);
                    var currentVolume = playerInfo["device"]["volume_percent"].Value<int>();
                    return currentVolume;
                }
                else
                {
                    throw new Exception($"Error fetching current volume: {response.ReasonPhrase}");
                }
            }
        }

        private async Task SetVolumeAsync(int volume)
        {
            await EnsureAccessTokenValid();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var request = new HttpRequestMessage(HttpMethod.Put, $"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}");
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error setting volume: {response.ReasonPhrase}");
                }
            }
        }
        private async Task EnsureAccessTokenValid()
        {
            if (DateTime.Now >= accessTokenExpirationTime)
            {
                Debug.Log("Access Token Expired. Refreshing...");
                await RefreshAccessToken();
            }
        }

        private async Task RefreshAccessToken()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                });

                request.Content = requestBody;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenInfo = JObject.Parse(responseBody);
                accessToken = tokenInfo["access_token"].ToString();

                int expiresIn = tokenInfo["expires_in"].Value<int>();
                accessTokenExpirationTime = DateTime.Now.AddSeconds(expiresIn);

                Debug.Log("Access Token Refreshed: REDACTED(##########)");
            }
        }
        public async Task<List<string>> GetUpNextPlaylistAsync()
        {
            List<string> upNextPlaylist = new List<string>();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    HttpResponseMessage response = await httpClient.GetAsync("https://api.spotify.com/v1/me/player/queue");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var jsonObject = JObject.Parse(result);

                        // Get the queue items
                        var queueItems = jsonObject["queue"];

                        // Iterate over the next 5 items or all items if less than 5
                        for (int i = 0; i < Math.Min(queueItems.Count(), 5); i++)
                        {
                            string trackName = (string)queueItems[i]["name"];
                            upNextPlaylist.Add(trackName);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch up next playlist: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return upNextPlaylist;
        }

    }
}