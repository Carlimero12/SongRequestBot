using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Security;
namespace SongRequestBot;

public class YTPlaylistManagment
{
    

    public static YouTubeService youTubeService()
    {
        YouTubeService temp;
        temp = Task<YouTubeService>.Run(async () => 
        {
            UserCredential TMPcredential;
            YouTubeService serviceTMP;
            using (FileStream stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                TMPcredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,

                    new[] { YouTubeService.Scope.Youtube },
                    "user1234",
                    CancellationToken.None,
                    new FileDataStore("YoutubePlaylistManager")
                );
                serviceTMP = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = TMPcredential,
                    ApplicationName = "SongRequestManager"
                });
            }
            return serviceTMP;
        }).Result;
        return temp;
    }

    public static async Task<bool> RemoveFromPlaylist(PlaylistItem item, bool bypass=false, string itemID = "fallb")
    {
        if (Program.DevMode)
        {
            return false;
        }
        string removeVidID = itemID;
        if (itemID == "fallb")
        {
            removeVidID = TwitchClientAPI.VidIdList.ElementAt(1).Id.ToString();
        }
        Console.WriteLine("\nTrying to Remove the thingy");
        var newPlaylistItem = new PlaylistItem();
        newPlaylistItem.Snippet = new PlaylistItemSnippet();
        newPlaylistItem.Snippet.PlaylistId = TLIBSecrets.config().ytPlaylist;
        await youTubeService().PlaylistItems.Delete(removeVidID).ExecuteAsync();
        TwitchClientAPI.VidIdList.Remove(item);
        return true;
    }

    public static async Task<string> ClearPlaylist(bool isTwitch = true)
    {
        if (Program.DevMode)
        {
            return "fallb";
        }
        var nextPageToken = "";
        while (nextPageToken != null)
        {
            var playlistItemsListRequest = youTubeService().PlaylistItems.List("snippet");
            playlistItemsListRequest.PlaylistId = TLIBSecrets.config().ytPlaylist;
            playlistItemsListRequest.MaxResults = 100;
            playlistItemsListRequest.PageToken = nextPageToken;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
            int itemRemoved = 0;
            if (isTwitch) await TwitchClientAPI.client.SendMessageAsync(TLIBSecrets.config().StreamerName, "Clearing the queue. This will take a bit due to API limitations. Requests will be closed till finished");
            TwitchClientAPI.songrequests = false;
            Stopwatch timer = new();
            timer.Start();
            foreach (var playlistItem in playlistItemsListResponse.Items)
            {
                await youTubeService().PlaylistItems.Delete(playlistItem.Id).ExecuteAsync();
                TwitchClientAPI.VidIdList.Remove(playlistItem);
                itemRemoved++;
                await Task.Delay(100);
                Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
            }
            timer.Stop();
            TwitchClientAPI.songrequests = true;
            if (isTwitch) await TwitchClientAPI.client.SendMessageAsync(TLIBSecrets.config().StreamerName, $"Requests Reopened after {timer.ElapsedMilliseconds / 1000}s");
            nextPageToken = playlistItemsListResponse.NextPageToken;
            if (isTwitch) await TwitchClientAPI.client.SendMessageAsync(TLIBSecrets.config().StreamerName, $"Removed {itemRemoved} from the queue");
            return $"Removed {itemRemoved} from the queue.\n Time Elapsed: {timer.ElapsedMilliseconds / 1000}s";
        }
        return "If this message is returned its a error. Contact @<1201127619299655741>";
    }

    public static async Task CountPlaylist()
    {
        if (Program.DevMode)
        {
            return;
        }
        var nextPageToken = "";
        while (nextPageToken != null)
        {
            var playlistItemsListRequest = youTubeService().PlaylistItems.List("snippet");
            playlistItemsListRequest.PlaylistId = TLIBSecrets.config().ytPlaylist;
            playlistItemsListRequest.MaxResults = 100;
            playlistItemsListRequest.PageToken = nextPageToken;
            // Retrieve the list of videos uploaded to the authenticated user's channel.
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
            //int counter = 0;
            foreach (var playlistItem in playlistItemsListResponse.Items)
            {
              // Print information about each video.
                /*counter +=1;
                if (counter >= TLIBSecrets.config().maxSongs)
                {
                    try
                    {
                        await youTubeService().PlaylistItems.Delete(playlistItem.Id).ExecuteAsync();
                    } catch (Exception ex)
                    {
                        string line = System.Environment.NewLine;
                        File.AppendAllText("Errors.txt", "-----------" + line + ex + line);
                    }
                } else
                {*/
                    TwitchClientAPI.VidIdList.Add(playlistItem);
                //}
                Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
            }
            nextPageToken = playlistItemsListResponse.NextPageToken;
        }
    }

    public static async Task<string> AddToPlaylist(string VidID="GNRMeaz6QRI")
    {
        if (Program.DevMode)
        {
            return "InDevMode";
        }
        if (Program.QueueEmpty && (TwitchClientAPI.VidIdList.Count > 2))
        {
            Program.QueueEmpty = false;
        }
        PlaylistItem newPlaylistItem = new PlaylistItem();
        newPlaylistItem.Snippet = new PlaylistItemSnippet();
        newPlaylistItem.Snippet.PlaylistId = TLIBSecrets.config().ytPlaylist;
        newPlaylistItem.Snippet.ResourceId = new ResourceId();
        newPlaylistItem.Snippet.ResourceId.Kind = "youtube#video";
        newPlaylistItem.Snippet.ResourceId.VideoId = VidID;
        // int.TryParse(newPlaylistItem.ContentDetails.EndAt, out int result);
        // if ( result > TLIBSecrets.config().MaxVidLenghtSec)
        // {
        //     return "Too long of a Song";
        // }
        newPlaylistItem = await youTubeService().PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
        string id = newPlaylistItem.Id;
        TwitchClientAPI.VidIdList.Add(newPlaylistItem);
        return newPlaylistItem.Snippet.Title;
    }
}