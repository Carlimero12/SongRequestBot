using NetCord;
using NetCord.Services;
using NetCord.Services.Commands;
using NetCord.Services.ApplicationCommands;

namespace SongRequestBot;
public enum SongRequestState
{
    Close,
    Check,
    [SlashCommandChoice(Name = "Open")]
    Open,
}
public class ExampleModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("addsong", "Pong!")]
    public string Pong([SlashCommandParameter(Name = "link", Description = "The Link to the YT Vid")] string @link)
    {
        User user = Context.User;
        ulong ChannelId = Context.Channel.Id;
        if (link == null)
        {
            return "Must include Link";
        }
        Task<string> s = Task.Run(async () => await DiscordClientApi.AddSong_Command(link, user, ChannelId));
        return s.Result;
    }

    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("clearqueue", "Clears the Playlist",
        DefaultGuildPermissions = Permissions.Administrator,
        Contexts = [InteractionContextType.Guild])]
    public string ClearQueue([SlashCommandParameter(Name = "confirmation", Description = "Type confirm to confirm the process")] string @confirmation)
    {
        if(confirmation.ToLower() != "confirm")
        {
            return "Run the command with confirm to confirm";
        }
        Task<string> s = Task.Run(async () => await YTPlaylistManagment.ClearPlaylist(isTwitch: false));
        
        return s.Result;
    }

    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("requeststatus", "Sets the Requests to open or Closed",
        DefaultGuildPermissions = Permissions.Administrator,
        Contexts = [InteractionContextType.Guild])]
    public string toggleRequests(SongRequestState state = SongRequestState.Check)
    {
        if (state.ToString() == "Close")
        {
            TwitchClientAPI.songrequests = false;
            return "Closed Requests";
        } else if (state.ToString() == "Open")
        {
            TwitchClientAPI.songrequests = true;
            return "Opened Requests";
        } else
        {   
            return $"SongRequests are {TwitchClientAPI.songrequests}".Replace("True", "opened").Replace("False","closed");
        }
    }

    [RequireContext<ApplicationCommandContext>(RequiredContext.DM)]
    [SlashCommand("whitelistguild", "Whitelists a Guild")]
    public string whitelistGuild(string GuildIdStr)
    {
        ulong GuildId = ulong.Parse(GuildIdStr);
        if (Context.User.Id != 1201127619299655741)
        {
            return "No permission to do this";
        }
        DiscordClientApi.GuildWhitelist.Add(GuildId);
        return $"Added {GuildId} to whitelist";
    }
}
