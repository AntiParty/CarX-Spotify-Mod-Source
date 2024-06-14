//Before getting started make sure you have added References
// Here are the References you will need
// From the Drift Racing Online_Data/Managed folder

//Assembly - CSharp.dll
//UnityEngine.dll
//UnityEngine.CoreModule.dll

//From the ZML/core folder
//ZML.API.dll

// ZML Doc Links - https://zi9.github.io/zml/docs/mod-dev-quick-start/
// https://zi9.github.io/zml/docs/api-docs/

using MyCoolMod;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using ZML.API;

namespace SpotifyMod
{
    [ZMLMod("zml.anti.spotify", "[ASI] Anti Spotify integration ", "1.0.0.3", "anti")]
    public class SpotifyMod : BaseMod, IToolboxUI
    {
        private const string ConfigFilePath = "config.json";
        private Assembly assembly_;

        // Store Spotify client ID and client secret
        private string clientId;
        private string clientSecret;
        private List<string> songIds;

        private string songInfoLabel;

        // Initialize SpotifyIntegration
        private SpotifyIntegration spotifyIntegration;
        private TrackFetcher trackFetcher;
        string githubRawUrl = "https://raw.githubusercontent.com/AntiParty/CarX-Spotify-Mod/main/trackids.json";
        private string configFilePath;
        private Queue<string> upNextPlaylist = new Queue<string>();
        private bool displayUpNext = false;
        private List<string> displayedUpNextPlaylist;
        private Vector2 scrollPosition = Vector2.zero;

        public SpotifyMod()
        {
            ZMLAPI.Keybinds.RegisterKey(this, "Start Song", KeyCode.None, KeyCode.None, StartSong);
            ZMLAPI.Keybinds.RegisterKey(this, "Pause Song", KeyCode.None, KeyCode.None, PauseSong);
            ZMLAPI.Keybinds.RegisterKey(this, "Skip Song", KeyCode.None, KeyCode.None, skipSong);
            ZMLAPI.Keybinds.RegisterKey(this, "Previous Song", KeyCode.None, KeyCode.None, PreviousSong21);
            ZMLAPI.Keybinds.RegisterKey(this, "Increase Volume", KeyCode.None, KeyCode.None, IncreaseVolume);
            ZMLAPI.Keybinds.RegisterKey(this, "Decrease Volume", KeyCode.None, KeyCode.None, DecreaseVolume);
            LoadConfig();
            configFilePath = Path.Combine(ZMLAPI.Paths.ModsPath, "config.json");
            SpotifyIntegration spotify = new SpotifyIntegration(clientId, clientSecret, githubRawUrl);
        }
        private void LoadConfig()
        {
            try
            {
                string modsFolderPath = ZMLAPI.Paths.ModsPath;
                Debug.Log($"Mods folder path: {modsFolderPath}");

                configFilePath = Path.Combine(modsFolderPath, "config.json");
                if (!File.Exists(configFilePath))
                {
                    // Create a default config file if it does not exist
                    var defaultConfig = new
                    {
                        ClientId = "your-spotify-client-id",
                        ClientSecret = "your-spotify-client-secret"
                    };
                    File.WriteAllText(configFilePath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
                    Debug.Log("Default config.json created. Please update it with your Spotify credentials.");
                }

                string configContent = File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<SpotifyConfig>(configContent);



                if (config != null && !string.IsNullOrEmpty(config.ClientId) && !string.IsNullOrEmpty(config.ClientSecret))
                {
                    spotifyIntegration = new SpotifyIntegration(config.ClientId, config.ClientSecret, githubRawUrl);
                    Debug.Log("SpotifyIntegration initialized successfully.");
                }
                else
                {
                    Debug.LogError("Invalid config.json format. Please ensure it contains 'ClientId' and 'ClientSecret'.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load config.json: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new Config
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                };

                var configJson = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, configJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save config: {ex.Message}");
            }
        }

        // Define the configuration structure
        private class Config
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }

        // GUI FUNCTIONS (ALL THE BUTTONS FOR THE MENU)
        // 
        //
        public async void OnToolboxGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("!---<color=red>AUTH NEEDED!</color>---!");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("!--Spotify auth--!"))
            {
                StartSpotifyAuth();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip Spotify Song"))
            {
                SkipSpotifySong();
            }
            if (GUILayout.Button("Previous Song"))
            {
                PreviousSong();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<color=red>Pause song</color>"))
            {
                PauseSong();
            }
            if (GUILayout.Button("<color=green>Start song</color>"))
            {
                StartSong();
            }

            if (GUILayout.Button("<color=#787578>Surprise me ;)</color>"))
            {
                PlayrandomSongAsync();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Increase Volume"))
            {
                IncreaseVolume();
            }
            if (GUILayout.Button("Decrease Volume"))
            {
                DecreaseVolume();
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Display up next"))
            {
                var upNextPlaylist = await spotifyIntegration.GetUpNextPlaylistAsync();
                DisplayUpNext(upNextPlaylist);
                displayUpNext = true; // Set flag to display up next playlist
            }
            if (displayUpNext && displayedUpNextPlaylist != null && displayedUpNextPlaylist.Count > 0)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                foreach (var song in displayedUpNextPlaylist)
                {
                    GUILayout.Label(song);
                }
            }

            GUILayout.Label($"SpotifyMod v{Version}");
        }
        // END OF GUI BUTTONS

        public void OnToolboxOpen()
        {
            // Leave this function empty if you don't need it or initialize some stuff here if you want
            // You must however declare it because it is an interface

        }

        // Not working (Might delete this whole)
        public async void UpdateSongInfoLabel()
        {
            string currentlyPlaying = await spotifyIntegration.GetCurrentlyPlayingAsync();
            songInfoLabel = currentlyPlaying; // Update the Text element with the song info
        }


        // start of voids for buttons
        private void StartSpotifyAuth()
        {
            if (spotifyIntegration != null)
            {
                spotifyIntegration.StartAuthProcess().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Spotify authentication failed: {task.Exception?.GetBaseException().Message}");
                    }
                    else
                    {
                        Debug.Log("Spotify authentication succeeded.");
                    }
                });
            }
            else
            {
                Debug.LogError("SpotifyIntegration is not initialized. Please check your config.json.");
            }
        }

        // Used for dev testing (checking if spotify works/logs)
        private async void LogSpotifySong()
        {
            try
            {
                var currentlyPlaying = await spotifyIntegration.GetCurrentlyPlayingAsync();
                Debug.Log(currentlyPlaying);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to fetch currently playing song: {ex.Message}");
            }
        }

        private async void IncreaseVolume()
        {
            try
            {
                await spotifyIntegration.IncreaseVolumeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to increase volume: {ex.Message}");
            }
        }
        private async void DecreaseVolume()
        {
            try
            {
                await spotifyIntegration.DecreaseVolumeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to Decrease volume: {ex.Message}");
            }
        }
        private async void SkipSpotifySong()
        {
            try
            {
                await spotifyIntegration.SkipSongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to skip the song: {ex.Message}");
            }
        }

        private async void PreviousSong()
        {
            try
            {
                await spotifyIntegration.PreviousSongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to skip the song: {ex.Message}");
            }
        }
        private async void PauseSong()
        {
            try
            {
                await spotifyIntegration.PauseSongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to skip the song: {ex.Message}");
            }
        }

        private async void StartSong()
        {
            try
            {
                await spotifyIntegration.StartSongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to skip the song: {ex.Message}");
            }
        }
        //Skip Song
        public void skipSong()
        {
            SkipSpotifySong();
        }
        //Go back 
        public void PreviousSong21()
        {
            PreviousSong();
        }
        //Start Song
        public void StartSong1()
        {
            StartSong();
        }

        //Pause song
        public void PauseSong1()
        {
            PauseSong();
        }
        public async void FetchSongList()
        {
            trackFetcher = new TrackFetcher(githubRawUrl);
            var trackIds = await trackFetcher.FetchTrackIdsAsync();
        }

        // Fetching song Id's from GitHub raw txt
        private async void PlayrandomSongAsync()
        {
            try
            {
                await spotifyIntegration.RandomSongAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start random song: {ex.Message}");
            }
        }
        //end of voids for buttons

        public async void DisplayUpNext(List<string> upNextPlaylist)
        {
            // Display the up next playlist in the UI or log it
            try
            {
                int count = 0;
                foreach (var song in upNextPlaylist)
                {
                    if (count >= 5)
                        break;

                    Debug.Log(song);
                    count++;
                }

                if (count == 0)
                {
                    Debug.Log("Up Next playlist is empty.");
                }

            }
            catch (Exception ex)
            {
                // Handle any exceptions
            }
        }
        public override void OnDeinitialize()
        {
            // Save Spotify client ID and client secret to config on game exit
            SaveConfig();
        }
        public class SpotifyConfig
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }
    }
}