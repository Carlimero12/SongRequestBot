using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.RegularExpressions;

using SongRequestBot;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using System.Net.Mail;

var loggerFactory = LoggerFactory.Create(c => c
    .AddConsole()
    //.SetMinimumLevel(LogLevel.Trace) // uncomment to view raw messages received from twitch
); 
GlobalExceptionHandler errorHandler = new GlobalExceptionHandler();
AppDomain.CurrentDomain.UnhandledException += async (s, e) => await errorHandler.TryHandleAsync(null, (Exception)e.ExceptionObject, CancellationToken.None);
TaskScheduler.UnobservedTaskException += async (s, e) => { await errorHandler.TryHandleAsync(null, e.Exception, CancellationToken.None); e.SetObserved(); };

ConnectionCredentials credentials = new ConnectionCredentials(TLIBSecrets.config().username, TLIBSecrets.config().token); // anonymous user, add Username and OAuth token to get the ability to send messages
TwitchClient client = new TwitchClient(loggerFactory: loggerFactory)
{
    ChatCommandIdentifiers = { "!", "$", "&" },
};

bool songrequests = true;
List<string> VidIdList = [];
List<string> bannedUsers = ["ronaldontwitch", "carlimero12"];

UserCredential credential;
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
}
YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
{
  HttpClientInitializer = credential,
  ApplicationName = "SongRequestManager"
});

client.Initialize(credentials);
client.OnConnected += Client_OnConnected;
client.OnMessageReceived += Client_OnMessageReceived;
client.OnChatCommandReceived += Client_OnChatCommandReceived;
client.OnJoinedChannel += Client_OnJoinedChannel;
await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);

async Task Client_OnConnected(object? sender, OnConnectedEventArgs e)
{
    
    await client.JoinChannelAsync(TLIBSecrets.config().StreamerName);
}

async Task Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
{
    await CountPlaylist();
    Console.WriteLine($"Connected to {e.Channel}");
    if (e.Channel != TLIBSecrets.config().StreamerName)
    {
        await client.SendMessageAsync(e.Channel, $"Switched to this Channel");
    } else {
        await client.SendMessageAsync(e.Channel, $"SongRequests 1.0 Started Up");
        Console.WriteLine($"\nSongRequests 1.0 Started Up");
    }
}
async Task Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
{
    string Link = e.ChatMessage.Message;
    Console.WriteLine($"{e.ChatMessage.Username}#{e.ChatMessage.Channel}: {e.ChatMessage.Message}");
    if (bannedUsers.Contains(e.ChatMessage.Username) && Link.Contains("youtu") && !Link.Contains("?list"))
    {
        await client.SendMessageAsync(e.ChatMessage.Channel, $"@{e.ChatMessage.Username} You have been banned from song requests!");
        return;
    }
    Regex YoutubeVideoIDRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
    Match youtubeMatchID = YoutubeVideoIDRegex.Match(Link);
    string MATCHID = youtubeMatchID.Groups[1].Value;
    if (MATCHID.Length != 11)
    {
        MATCHID = "fallb";
    }
    Console.Write($"ResultOfRegex :{MATCHID}\n");
    

    if (songrequests && MATCHID != "fallb" && (Link.Contains('§') || e.ChatMessage.IsBroadcaster))
    {
        try
        {
            try
            {   
                int IdListLenght = VidIdList.Count;
                if (IdListLenght >= TLIBSecrets.config().maxSongs)
                {
                    await RemoveFromPlaylist();
                }
                await AddToPlaylist(youtubeMatchID.Groups[1].Value);
                await client.SendMessageAsync(e.ChatMessage.Channel, "Added song to the list!");
            } catch (Exception ex)
            {
                Console.WriteLine("\nA error occured :{0}", ex);
                await client.SendMessageAsync(e.ChatMessage.Channel, $"Error occured, please contact Carlimero12.");
                //await client.SendMessageAsync(e.ChatMessage.Channel, "Exception: " + ex);              
            }

        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", ex.Message);
        }
    } else if (songrequests==false && Link.Contains("youtu") && Link.Contains('§') && MATCHID != "fallb")
    {
        await client.SendMessageAsync(e.ChatMessage.Channel, $"Songrequests aren't Open @{e.ChatMessage.Username}!");
    } else if (Link.Contains("youtu") && Link.Contains("?list") && Link.Contains('§') && MATCHID != "fallb")
    {
        await client.SendMessageAsync(e.ChatMessage.Channel, $"@{e.ChatMessage.Username} Links have not been Implemented/Used yet!");
    } else
    {
        return;
    }
}

async Task<bool> RemoveFromPlaylist(bool bypass=false, string itemID = "fallb")
{
    try
    {
        if (itemID == "fallb")
        {
            VidIdList.ElementAt(0).ToString();
        }
        Console.WriteLine("\nTrying to Remove the thingy");
        string removeVidID = itemID;
        var newPlaylistItem = new PlaylistItem();
        newPlaylistItem.Snippet = new PlaylistItemSnippet();
        newPlaylistItem.Snippet.PlaylistId = TLIBSecrets.config().ytPlaylist;
        try
        {
            await youtubeService.PlaylistItems.Delete(removeVidID).ExecuteAsync();
            VidIdList.Remove(removeVidID);
        } catch (Exception ex)
        {
            removeVidID = VidIdList.ElementAt(1).ToString();
            string line = System.Environment.NewLine;
            try {
                await youtubeService.PlaylistItems.Delete(removeVidID).ExecuteAsync();
                VidIdList.Remove(removeVidID);
            } catch (Exception ex2)
            {
                await CountPlaylist();
                File.AppendAllText("Errors.txt", "-----------" + DateTime.Now.ToString() + line + ex2 + line);
                await client.SendMessageAsync(TLIBSecrets.config().StreamerName, $"There has been an error removing the Songs that are too much. If this messages returns please run '!clearRequests confirm' and restart me after");
                return false;
            }
        }
    } catch (IndexOutOfRangeException)
    {
        Console.Write("\nItem doesnt exist");
    }
    return true;
}

async Task ClearPlaylist()
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
        await client.SendMessageAsync(TLIBSecrets.config().StreamerName, $"Removed {itemRemoved} from the queue");
    }
    
    VidIdList.Clear();
}

async Task CountPlaylist()
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
                VidIdList.Add(playlistItem.Id);
            }
            Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
        }
        nextPageToken = playlistItemsListResponse.NextPageToken;
    }
}

async Task AddToPlaylist(string VidID="GNRMeaz6QRI")
{
    var newPlaylistItem = new PlaylistItem();
    newPlaylistItem.Snippet = new PlaylistItemSnippet();
    newPlaylistItem.Snippet.PlaylistId = TLIBSecrets.config().ytPlaylist;
    newPlaylistItem.Snippet.ResourceId = new ResourceId();
    newPlaylistItem.Snippet.ResourceId.Kind = "youtube#video";
    newPlaylistItem.Snippet.ResourceId.VideoId = VidID;
    newPlaylistItem = await youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
    string id = newPlaylistItem.Id;
    VidIdList.Add(id);
}

async Task Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
{
    var channel = e.ChatMessage.Channel;
    var command = e.Command;

    switch (command.Name.ToLower())
    {
        case "clearqueue":
            try
            {
                if (!e.ChatMessage.IsBroadcaster && !e.ChatMessage.UserDetail.IsModerator)
                {
                    await client.SendMessageAsync(channel, $"@{e.ChatMessage.Username} You dont have Permission to do this");
                    return;
                }
                if (command.ArgumentsAsList.Count() == 0)
                {
                    await client.SendMessageAsync(channel, $"""@{e.ChatMessage.Username} Type "!clearqueue confirm" to delete all songs in Queue""");
                    return;
                } else if (command.ArgumentsAsList[0].ToString().ToLower() == "confirm")
                {
                    await ClearPlaylist();
                } else return;
            } catch (Exception ex)
            {
                Console.WriteLine("Error: "+ ex);
            }
            break;
        case "songrequests":
            if (!e.ChatMessage.IsBroadcaster && !e.ChatMessage.UserDetail.IsModerator)
            {
                await client.SendMessageAsync(channel, "Only Streamer and Mods can toggle SongRequests!");
                return;
            }
            string responseMSG = "Off";
            if (songrequests) { songrequests = false; responseMSG = "are closed now!"; }
            else  { songrequests = true; responseMSG = "are open now! Put any link in chat with § in the message and it will be detected"; }
            await client.SendMessageAsync(channel, "SongRequests " + responseMSG);
            break;
        case "info":
            await client.SendMessageAsync(channel, "Hi, I am a bot created for Saving Songrequests");
            break;
        case "leave":
            if (!e.ChatMessage.IsBroadcaster)
                return;
            await client.SendMessageAsync(channel, "Goodbye! I am leaving the channel.");
            await client.LeaveChannelAsync(channel);
            break;
        case "help":
            if (command.ArgumentsAsList.Count is 0)
            {
                var helpText = "Available commands: !info, !repeat <message>, !help [command]";
                if (e.ChatMessage.IsBroadcaster)
                    helpText += ", !leave";
                    helpText += ", !songrequests";
                    helpText += ", !clearqueue";

                await client.SendMessageAsync(channel, helpText);
                return;
            }
            switch (command.ArgumentsAsList[0])
            {
                case "clearqueue":
                    if (!e.ChatMessage.IsBroadcaster && !e.ChatMessage.UserDetail.IsModerator)
                    {
                        await client.SendMessageAsync(channel, "You dont have Permission for this Command");
                        return;
                    }
                    await client.SendMessageAsync(channel, "The !clearqueue command clears the Youtube Playlist");
                    break;
                case "songrequests":
                    await client.SendMessageAsync(channel, "The !songrequests command toggles SongRequests open / closed.");
                    break;
                case "info":
                    await client.SendMessageAsync(channel, "The !info command provides information about the bot.");
                    break;
                case "help":
                    await client.SendMessageAsync(channel, "The !help command provides information about available commands.");
                    break;
                case "leave":
                    if (!e.ChatMessage.IsBroadcaster)
                    {    return;  }
                    await client.SendMessageAsync(channel, "The !leave command makes the bot leave the channel.");
                    break;
                default:
                    await client.SendMessageAsync(channel, $"No help available for command: {command.ArgumentsAsList[0]}");
                    break;
            }
            break;
        default:
            Console.WriteLine($"Unknown command: {command.Name}");
            break;
    }
}