using System;
using System.Text.RegularExpressions;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Google;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
namespace SongRequestBot;

public class TwitchClientAPI
{
    private static ILoggerFactory loggerFactory = LoggerFactory.Create(c => c
        .AddConsole()
        //.SetMinimumLevel(LogLevel.Trace) // uncomment to view raw messages received from twitch
    );
    private static ConnectionCredentials Twcredentials = new ConnectionCredentials(TLIBSecrets.config().username, TLIBSecrets.config().token);
    public static TwitchClient client = new TwitchClient(loggerFactory: loggerFactory)
    {
        ChatCommandIdentifiers = { "!", "?"},
    };
    public static List<string> lastID = [];

    public static List<string> bannedUsers = ["ronaldontwitch", "trash_pcplayer"];
    public static bool songrequests = true;
    public static List<PlaylistItem> VidIdList = [];
    public TwitchClientAPI()
    {
    }

    public static async void StaticMethod()
    {
        client.Initialize(Twcredentials);
        client.OnChatCommandReceived += Client_OnChatCommandReceived;
        client.OnConnected += Client_OnConnected;
        client.OnUserTimedout += Client_OnTimeoutReceived;
        client.OnMessageReceived += Client_OnMessageReceived;
        client.OnJoinedChannel += Client_OnJoinedChannel;
        await client.ConnectAsync();
    }

    static async Task Client_OnConnected(object? sender, OnConnectedEventArgs e)
    {

        await client.JoinChannelAsync(TLIBSecrets.config().StreamerName);
    }

    static async Task Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine($"Connected to {e.Channel}");

        Console.WriteLine($"\nStarted as {e.BotUsername}. Youtube API linked. send Messages containing youtube links for any video and it will be added to the queue. Dont spam.");
        if (Program.DevMode)
        {
            await client.SendMessageAsync(e.Channel, $"Warning DevMode is active. Youtube API calls will not connect. Disable this is used on a non-Dev stream.");
        }
        else {
            await client.SendMessageAsync(e.Channel, $"\nSongrequest.");
        }
        await YTPlaylistManagment.CountPlaylist();
    }

    static async Task Client_OnTimeoutReceived(object? sender, OnUserTimedoutArgs e)
    {
        if (!(e.UserTimeout.Username == TLIBSecrets.config().username))
        {
            return;
        }
        int timeouttime = Int32.Parse(e.UserTimeout.TimeoutDuration.TotalMilliseconds.ToString());
        Console.WriteLine($"I have been Timeouted on {e.UserTimeout.Channel} for {timeouttime / 1000 }seconds");
        await Task.Delay(timeouttime + 500);
        await client.SendMessageAsync(e.UserTimeout.Channel, $"Timeout over; Ready again. DevMode: {Program.DevMode}");
    }

    static async Task Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        string[] arguments = e.ChatMessage.Message.Split(" ");
        Console.WriteLine($"{e.ChatMessage.Username}#{e.ChatMessage.Channel}: {e.ChatMessage.Message}");
        if (arguments[0].ToLower() != "!sr")
        {
            return;
        }
        if (Program.SubVipMode && !e.ChatMessage.UserDetail.IsVip && !e.ChatMessage.UserDetail.IsSubscriber && !e.ChatMessage.UserDetail.IsModerator && !e.ChatMessage.IsBroadcaster)
        {
            await client.SendMessageAsync(e.ChatMessage.Channel, $"@{e.ChatMessage.Username} Sub/Vip mode is active!");
            return;
        }
        string Link = e.ChatMessage.Message;

        Regex YoutubeVideoIDRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
        Match youtubeMatchID = YoutubeVideoIDRegex.Match(Link);

        string MATCHID = youtubeMatchID.Groups[1].Value;
        if (MATCHID.Length != 11)
        {
            await client.SendMessageAsync(e.ChatMessage.Channel, $"@{e.ChatMessage.Username} Invalid Link");
            return;
        }
        Console.Write($"ResultOfRegex :{MATCHID}\n");
        if (songrequests)
        {
            if (bannedUsers.Contains(e.ChatMessage.Username))
            {
                await client.SendMessageAsync(e.ChatMessage.Channel, $"@{e.ChatMessage.Username} You have been banned from song requests!");
                return;
            }
            if (lastID.Contains(MATCHID) && !Program.authUsers.Contains(e.ChatMessage.Username.ToLower()))
            {
                if (lastID.Count() >= 10)
                {
                    lastID.Clear();
                }
                await client.SendMessageAsync(e.ChatMessage.Channel, $"Songrequest already requested!");
                return;
            }
            lastID.Add(MATCHID);
            try
            {
                int IdListLenght = VidIdList.Count;
                if (IdListLenght >= TLIBSecrets.config().maxSongs && TLIBSecrets.config().maxSongs != -1)
                {
                    await YTPlaylistManagment.RemoveFromPlaylist(item: VidIdList.ElementAt(0));
                }
                string name = await YTPlaylistManagment.AddToPlaylist(youtubeMatchID.Groups[1].Value);
                Console.WriteLine(name);
                await client.SendMessageAsync(e.ChatMessage.Channel, $"Added {name} to queue!");
            } 
            catch (GoogleApiException ex)
            {
                await client.SendMessageAsync(e.ChatMessage.Channel, $"@{e.ChatMessage.Username} [Api Error]");
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", ex.Message);
                await client.SendMessageAsync(e.ChatMessage.Channel, ex.Message);
            }
        } else if (songrequests==false)
        {
            await client.SendMessageAsync(e.ChatMessage.Channel, $"Songrequests aren't Open!");
        } else
        {
            await client.SendMessageAsync(e.ChatMessage.Channel, $"Something Went Wrong!");
            return;
        }
    }

    static async Task Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var channel = e.ChatMessage.Channel;
        var command = e.Command;

        switch (command.Name.ToLower())
        {
            case "sr":
                break;
            case "submode":
                if (!e.ChatMessage.UserDetail.IsModerator && !e.ChatMessage.IsBroadcaster && !Program.authUsers.Contains(e.ChatMessage.Username.ToLower()))
                {
                    await client.SendMessageAsync(channel, $"@{e.ChatMessage.Username} You dont have Permission to perform this action. See all commands at !help");
                    return;
                }
                Program.SubVipMode = !Program.SubVipMode;
                await client.SendMessageAsync(channel, $"""@{e.ChatMessage.Username} Toggled SubOnly mode. State of var: {Program.SubVipMode}""");
                break;
            case "clearqueue":
                try
                {
                    if (!e.ChatMessage.IsBroadcaster && !e.ChatMessage.UserDetail.IsModerator && !Program.authUsers.Contains(e.ChatMessage.Username.ToLower()) || Program.QueueEmpty || VidIdList.Count() < 2)
                    {
                        await client.SendMessageAsync(channel, $"@{e.ChatMessage.Username} You dont have Permission to do this or the Queue is alr empty");
                        return;
                    }
                    if (command.ArgumentsAsList.Count() == 0)
                    {
                        await client.SendMessageAsync(channel, $"""@{e.ChatMessage.Username} Type "!clearqueue confirm" to delete all songs in Queue""");
                        return;
                    } else if (command.ArgumentsAsList[0].ToString().ToLower() == "confirm")
                    {
                        await YTPlaylistManagment.ClearPlaylist();
                        Program.QueueEmpty = true;
                    } else return;
                } catch (Exception ex)
                {
                    Console.WriteLine("Error: "+ ex);
                }
                break;
            case "songrequests":
                if (!e.ChatMessage.IsBroadcaster && !e.ChatMessage.UserDetail.IsModerator && !Program.authUsers.Contains(e.ChatMessage.Username.ToLower()))
                {
                    await client.SendMessageAsync(channel, "Only Streamer and Mods can toggle SongRequests!");
                    return;
                }
                string responseMSG = "Off";
                if (songrequests) { songrequests = false; responseMSG = "are closed now!"; }
                else  { songrequests = true; responseMSG = "are open now! Use !sr [youtubeLink] to add to the queue."; }
                await client.SendMessageAsync(channel, "SongRequests " + responseMSG);
                break;
            case "help":
                if (command.ArgumentsAsList.Count is 0)
                {
                    var helpText = "Available commands: !help [command]";
                    if (e.ChatMessage.IsBroadcaster || e.ChatMessage.UserDetail.IsModerator)
                    {
                        helpText += ", !songRequests";
                        helpText += ", !clearQueue";
                        helpText += ", !subMode";
                    }
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
                    case "help":
                        await client.SendMessageAsync(channel, "The !help command provides information about available commands.");
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
