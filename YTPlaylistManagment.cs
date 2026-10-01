using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
namespace SongRequestBot;

public class YTPlaylistManagment
{
    private UserCredential credential;
    
    
    private static YouTubeService youtubeService;
    public YTPlaylistManagment()
    {
        Task.Run(async () => { await GenerateCredentials(); });
    }

    public async Task GenerateCredentials()
    {
        using (FileStream stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                // This OAuth 2.0 access scope allows for full read/write access to the
                // authenticated user's account.
                new[] { YouTubeService.Scope.Youtube },
                "useracc",
                CancellationToken.None,
                new FileDataStore("YoutubePlaylistManager")
            );
            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "SongRequestManager"
            });
        }
    }
    
    public static async Task<bool> RemoveFromPlaylist(bool bypass=false, string itemID = "fallb")
    {
        try
        {
            if (itemID == "fallb")
            {
                TwitchClientAPI.VidIdList.ElementAt(0).ToString();
            }
            Console.WriteLine("\nTrying to Remove the thingy");
            string removeVidID = itemID;
            var newPlaylistItem = new PlaylistItem();
            newPlaylistItem.Snippet = new PlaylistItemSnippet();
            newPlaylistItem.Snippet.PlaylistId = TLIBSecrets.config().ytPlaylist;
            try
            {
                await youtubeService.PlaylistItems.Delete(removeVidID).ExecuteAsync();
                TwitchClientAPI.VidIdList.Remove(removeVidID);
            } catch (Exception ex)
            {
                removeVidID = TwitchClientAPI.VidIdList.ElementAt(1).ToString();
                string line = System.Environment.NewLine;
                try {
                    await youtubeService.PlaylistItems.Delete(removeVidID).ExecuteAsync();
                    TwitchClientAPI.VidIdList.Remove(removeVidID);
                } catch (Exception ex2)
                {
                    await CountPlaylist();
                    File.AppendAllText("Errors.txt", "-----------" + DateTime.Now.ToString() + line + ex2 + line);
                    await TwitchClientAPI.client.SendMessageAsync(TLIBSecrets.config().StreamerName, $"There has been an error removing the Songs that are too much. If this messages returns please run '!clearRequests confirm' and restart me after");
                    return false;
                }
            }
        } catch (IndexOutOfRangeException)
        {
            Console.Write("\nItem doesnt exist");
        }
        return true;
    }

    public static async Task ClearPlaylist()
    {
        var nextPageToken = "";
        while (nextPageToken != null)
        {
            var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
            playlistItemsListRequest.PlaylistId = TLIBSecrets.config().ytPlaylist;
            playlistItemsListRequest.MaxResults = 100;
            playlistItemsListRequest.PageToken = nextPageToken;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
            int itemRemoved = 0;
            foreach (var playlistItem in playlistItemsListResponse.Items)
            {
                try
                {
                    await youtubeService.PlaylistItems.Delete(playlistItem.Id).ExecuteAsync();
                    itemRemoved++;
                } 
                catch (Exception ex)
                {
                    string line = System.Environment.NewLine;
                    File.AppendAllText("Errors.txt", "-----------" + line + ex + line);
                }
                Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
            }
            nextPageToken = playlistItemsListResponse.NextPageToken;
            await TwitchClientAPI.client.SendMessageAsync(TLIBSecrets.config().StreamerName, $"Removed {itemRemoved} from the queue");
        }

        TwitchClientAPI.VidIdList.Clear();
    }

    public static async Task CountPlaylist()
    {
        var nextPageToken = "";
        while (nextPageToken != null)
        {
            var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
            playlistItemsListRequest.PlaylistId = TLIBSecrets.config().ytPlaylist;
            playlistItemsListRequest.MaxResults = 100;
            playlistItemsListRequest.PageToken = nextPageToken;
            // Retrieve the list of videos uploaded to the authenticated user's channel.
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
            int counter = 0;
            foreach (var playlistItem in playlistItemsListResponse.Items)
            {
              // Print information about each video.
                counter +=1;
                if (counter >= TLIBSecrets.config().maxSongs)
                {
                    try
                    {
                        await youtubeService.PlaylistItems.Delete(playlistItem.Id).ExecuteAsync();
                    } catch (Exception ex)
                    {
                        string line = System.Environment.NewLine;
                        File.AppendAllText("Errors.txt", "-----------" + line + ex + line);
                    }
                } else
                {
                    TwitchClientAPI.VidIdList.Add(playlistItem.Id);
                }
                Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
            }
            nextPageToken = playlistItemsListResponse.NextPageToken;
        }
    }

    public static async Task AddToPlaylist(string VidID="GNRMeaz6QRI")
    {
        var newPlaylistItem = new PlaylistItem();
        newPlaylistItem.Snippet = new PlaylistItemSnippet();
        newPlaylistItem.Snippet.PlaylistId = TLIBSecrets.config().ytPlaylist;
        newPlaylistItem.Snippet.ResourceId = new ResourceId();
        newPlaylistItem.Snippet.ResourceId.Kind = "youtube#video";
        newPlaylistItem.Snippet.ResourceId.VideoId = VidID;
        newPlaylistItem = await youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
        string id = newPlaylistItem.Id;
        TwitchClientAPI.VidIdList.Add(id);
    }
}