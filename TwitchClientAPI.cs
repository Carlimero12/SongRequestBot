using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;


namespace SongRequestBot;

public class TwitchClientAPI
{
    private static ILoggerFactory loggerFactory = LoggerFactory.Create(c => c
    .AddConsole()
    //.SetMinimumLevel(LogLevel.Trace) // uncomment to view raw messages received from twitch
    ); 
    private ConnectionCredentials Twcredentials;
    public static TwitchClient client;
    static List<string> bannedUsers = ["ronaldontwitch"];
    private static bool songrequests = true;
    public static List<string> VidIdList = [];
    public TwitchClientAPI()
    {
        Task.Run(async () => { 
            Twcredentials = new ConnectionCredentials(TLIBSecrets.config().username, TLIBSecrets.config().token);
            client = new TwitchClient(loggerFactory: loggerFactory)
            {
                ChatCommandIdentifiers = { "!", "?"},
            };
            client.Initialize(Twcredentials);
            client.OnConnected += Client_OnConnected;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            await client.ConnectAsync();
            await Task.Delay(Timeout.Infinite);
        });
    }

    static async Task Client_OnConnected(object? sender, OnConnectedEventArgs e)
    {

        await client.JoinChannelAsync(TLIBSecrets.config().StreamerName);
    }

    static async Task Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        await YTPlaylistManagment.CountPlaylist();
        Console.WriteLine($"Connected to {e.Channel}");
        if (e.Channel != TLIBSecrets.config().StreamerName)
        {
            await client.SendMessageAsync(e.Channel, $"Switched to this Channel");
        } else {
            await client.SendMessageAsync(e.Channel, $"SongRequests 1.0 Started Up");
            Console.WriteLine($"\nSongRequests 1.0 Started Up");
        }
    }
    static async Task Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
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
                        await YTPlaylistManagment.RemoveFromPlaylist();
                    }
                    await YTPlaylistManagment.AddToPlaylist(youtubeMatchID.Groups[1].Value);
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

    static async Task Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
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
                        await YTPlaylistManagment.ClearPlaylist();
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
}