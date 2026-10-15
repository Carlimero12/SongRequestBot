using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;
using System.Text.RegularExpressions;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using Newtonsoft.Json;

namespace SongRequestBot
{
public class DiscordClientApi()
{
    public static GatewayClient client = new(new BotToken(TLIBSecrets.config().discordToken), new GatewayClientConfiguration
    {
        Logger = new ConsoleLogger(),
        Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent | GatewayIntents.Guilds,
    });
    public static List<ulong> GuildWhitelist = [1294646380714917959, 788018968332861446];
    static ApplicationCommandService<ApplicationCommandContext> applicationCommandService = new();

    public static async void DiscordClientInitializer()
    {
        applicationCommandService.AddSlashCommand(new SlashCommandBuilder("ping", "Ping!", () => "Pong!"));
        client.InteractionCreate += async interaction =>
        {
            if (interaction is not ApplicationCommandInteraction applicationCommandInteraction)
                return;

            // Execute the command
            var result = await applicationCommandService.ExecuteAsync(new ApplicationCommandContext(applicationCommandInteraction, client));

            if (result is not IFailResult failResult)
                return;
            try
            {
                await interaction.SendResponseAsync(InteractionCallback.Message(failResult.Message));
            }
            catch
            {
            }
        };
        applicationCommandService.AddModules(typeof(Program).Assembly);
        await applicationCommandService.RegisterCommandsAsync(client.Rest, client.Id);
        client.GuildCreate += async args => {
            if (!GuildWhitelist.Contains(args.GuildId) && args.Guild.OwnerId != 610420159739330566)
            {
                MessageProperties props = new();
                props.Content = $"Following Guild has attempted to invite me: {args.Guild.Name}#{args.GuildId}.\nRun /whitelist";
                await client.Rest.SendMessageAsync(client.Rest.GetDMChannelAsync(1201127619299655741).Result.Id, props);
                props.Content = $"Your Server {args.Guild.Name} is not Whitelisted!";
                await client.Rest.SendMessageAsync(client.Rest.GetDMChannelAsync(args.Guild.OwnerId).Result.Id, props);
                await client.Rest.LeaveGuildAsync(args.GuildId);
            }
        };
        await client.StartAsync();

    } 

    public static async Task<string> AddSong_Command(string message, User user, ulong ChannelId)
    {
        string def =  "fallb";
        string Link = message;
        Regex YoutubeVideoIDRegex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
        Match youtubeMatchID = YoutubeVideoIDRegex.Match(Link);
        string MATCHID = youtubeMatchID.Groups[1].Value;
        if (Program.SubVipMode)
        {
            return $"@{user.Username} Sub/Vip mode is active and only Twitchside requests will be accepted rn!";
        }
        if (MATCHID.Length != 11)
        {
            return $"<@{user.Id}> Invalid Link";
        }    
        if (TwitchClientAPI.lastID.Contains(MATCHID) && !Program.authUsers.Contains(user.Username.ToLower()))
        {
            if (TwitchClientAPI.lastID.Count() >= 10)
            {
                TwitchClientAPI.lastID.Clear();
            }
            return $"Songrequest already requested!";
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
            return $"Added {name} to queue!";
        } 
        catch (Exception ex)
        {
            Console.WriteLine("\nException Caught!");
            return $"Error: " + ex.Message;
        }
    }
}
}
