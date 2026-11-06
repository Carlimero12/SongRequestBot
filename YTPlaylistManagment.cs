using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using TwitchLib.Api.Auth;
using System.Text.Json.Serialization;
using System.Text.Json;
namespace SongRequestBot;

public class YTPlaylistManagment
{
    
    public static YouTubeService youTubeService()
    {
        YouTubeService temp;
        temp = YoutubeCreds().Result;
        return temp;
    }

    private static async Task<YouTubeService> YoutubeCreds()
    {
        UserCredential TMPcredential;
        YouTubeService serviceTMP;
        using (FileStream stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
        {
            if(!File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}/YoutubePlaylistManager.json/Google.Apis.Auth.OAuth2.Responses.TokenResponse-{Program.YTProfileName}"))
            {
                TMPcredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube },
                    $"{Program.YTProfileName}",
                    CancellationToken.None,
                    new FileDataStore("YoutubePlaylistManager.json", true),
                    new LocalServerCodeReceiver()
                );
            } else
            {
                GoogleAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes = new[] { YouTubeService.Scope.Youtube },
                    DataStore = new FileDataStore("YoutubePlaylistManager.json", true),
                });
                TokenResponse token = await flow.LoadTokenAsync(Program.YTProfileName, CancellationToken.None);
                if (token == null)
                {
                    throw new NullReferenceException("Error authorizing from file.");
                }
                TMPcredential = new UserCredential(flow, Program.YTProfileName, token);
                
            }

            serviceTMP = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = TMPcredential,
                ApplicationName = "SongRequestManager"
            });
        }
        return serviceTMP;
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
                try {await youTubeService().PlaylistItems.Delete(playlistItem.Id).ExecuteAsync();}
                catch (Exception x)
                {
                    Console.WriteLine(x);
                    await Task.Delay(500);
                    continue;
                }
                TwitchClientAPI.VidIdList.Remove(playlistItem);
                itemRemoved++;
                Console.WriteLine("REMOVING {0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
                await Task.Delay(500);
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
