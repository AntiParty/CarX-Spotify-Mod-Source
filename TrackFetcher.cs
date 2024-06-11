using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace MyCoolMod
{
    public class TrackFetcher
    {
        private string githubRawUrl;

        public TrackFetcher(string githubRawUrl)
        {
            this.githubRawUrl = githubRawUrl;
        }

        public async Task<List<string>> FetchTrackIdsAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(githubRawUrl);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var trackIds = JsonConvert.DeserializeObject<List<string>>(content);

                    return trackIds;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to fetch track IDs: {ex.Message}");
                return new List<string>();
            }
        }
    }
}