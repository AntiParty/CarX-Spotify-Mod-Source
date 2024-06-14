using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
/********************************************************************************************/
//              NOT USED AT ALL CURRENTLY (DONT THINK THIS WILL BE USED)
//              Currently fetches version through  Assembly 
/********************************************************************************************/
public class VersionChecker
{
    private string githubRawUrl;

    public VersionChecker(string githubRawUrl)
    {
        this.githubRawUrl = githubRawUrl;
    }

    public async Task<string> CheckLatestVersionAsync()
    {
        try
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(githubRawUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return content.Trim(); // Assuming the version string is the only content in the file
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch latest version: {ex.Message}");
            return null;
        }
    }
}
