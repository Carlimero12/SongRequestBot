using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;
using System.Text.RegularExpressions;

namespace SongRequestBot
{
public class DiscordClientApi()
{
    public static GatewayClient client = new(new BotToken(TLIBSecrets.config().discordToken), new GatewayClientConfiguration
    {
        Logger = new ConsoleLogger(),
        Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,
    });

    public static async void DiscordClientInitializer()
        {
            client.MessageCreate += async message =>
            {
                await Client_OnMessageReceived(message);
            };
            await client.StartAsync();
        } 
    
    static async Task Client_OnMessageReceived(Message message)
    {
        string[] arguments = message.Content.Split(" ");
        Console.WriteLine($"{message.Author}#{message.Channel}: {message.Content}");
        if (arguments[0].ToLower() != "!sr")
        {
            return;
        }
        string Link = message.Content;
        
        Regex YoutubeVideoIDRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
        Match youtubeMatchID = YoutubeVideoIDRegex.Match(Link);

        string MATCHID = youtubeMatchID.Groups[1].Value;
        if (MATCHID.Length != 11)
        {
            MessageProperties props = new();
            props.Content = $"<@{message.Author.Id}> Invalid Link";
            await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
            return;
        }
        Console.Write($"ResultOfRegex :{MATCHID}\n");
        if (TwitchClientAPI.songrequests)
        {
            if (TwitchClientAPI.bannedUsers.Contains(message.Author.Username))
            {
                MessageProperties props = new();
                props.Content = $"<@{message.Author.Id}> You have been banned from song requests!";
                await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
                return;
            }
            if (TwitchClientAPI.lastID.Contains(MATCHID) && !Program.authUsers.Contains(message.Author.Username.ToLower()))
            {
                if (TwitchClientAPI.lastID.Count() >= 10)
                {
                    TwitchClientAPI.lastID.Clear();
                }
                MessageProperties props = new();
                props.Content = $"<@{message.Author.Id}> Songrequest already requested!";
                await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
                return;
            }
            TwitchClientAPI.lastID.Add(MATCHID);
            try
            {
                int IdListLenght = TwitchClientAPI.VidIdList.Count;
                if (IdListLenght >= TLIBSecrets.config().maxSongs && TLIBSecrets.config().maxSongs != -1)
                {
                    await YTPlaylistManagment.RemoveFromPlaylist();
                }
                string name = await YTPlaylistManagment.AddToPlaylist(youtubeMatchID.Groups[1].Value);
                Console.WriteLine(name);
                MessageProperties props = new();
                props.Content = $"<@{message.Author.Id}> Added {name} to queue!";
                await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
            } 
            catch (Google.GoogleApiException ex)
            {
                MessageProperties props = new();
                props.Content = $"<@{message.Author.Id}> [Api Error]";
                await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", ex.Message);
                MessageProperties props = new();
                props.Content = $"<@{message.Author.Id}> Error: " + ex.Message;
                await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
                Console.WriteLine(ex);
            }
        } else if (TwitchClientAPI.songrequests==false)
        {
            MessageProperties props = new();
            props.Content = $"<@{message.Author.Id}> Songrequests aren't Open!";
            await client.Rest.SendMessageAsync(channelId:message.ChannelId , props);
        }
        
    }
}
}
