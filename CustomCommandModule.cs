using NetCord;
using NetCord.Services;
using NetCord.Services.Commands;
using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Gateway;
using Google.Apis.YouTube.v3.Data;
using System.IO;

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
    [SlashCommand("addsong", "Adds a song to the Queue!",
        DefaultGuildPermissions = Permissions.SendMessages,
        Contexts = [InteractionContextType.Guild])]
    public string AddSong([SlashCommandParameter(Name = "link", Description = "The Link to the YT Vid")] string @link)
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
        if (Context == null || Context.Guild == null)
        {
            return "Null variable in required context";
        }
        if (!DiscordClientApi.GuildWhitelist.Contains(Context.Guild.Id))
        {
            return "Unauthorized Server.";
        }
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
    [SlashCommand("whitelistguild", "Whitelists a Guild", Contexts = [InteractionContextType.BotDMChannel])]
    public string WhitelistGuild(string GuildIdStr)
    {
        ulong GuildId = ulong.Parse(GuildIdStr);
        if (Context.User.Id != 1201127619299655741)
        {
            return "No permission to do this";
        }
        DiscordClientApi.GuildWhitelist.Add(GuildId);
        return $"Added {GuildId} to whitelist";
    }

    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("listsongs", "Gives a list of all current Songs in queue with links to them.", Contexts = [InteractionContextType.Guild])]
    public async Task<string> ListSongs()
    {
        string returnMsg = "If the list is cutoff, this is because of DiscordMessage limit. File summarys for trusted users are Coming soon.";
        string songlist = "";
        foreach(PlaylistItem item in TwitchClientAPI.VidIdList)
        {
            songlist += $"{item.Snippet.Title} -- https://youtube.com/watch?v={item.Id}\n";
        }
        MessageProperties msg = new()
        {
            Content = $"All Songs: {songlist[..1700]}\n-Cutoff"
        };
        /*if (Program.premiumUsers.Contains(Context.User.Username.ToLower()))
        {
            using (FileStream stream = File.Open("songlist.txt", FileMode.Open))
            {
                await File.WriteAllTextAsync("songrequests.txt", songlist);
                msg = new()
                {
                    Attachments = new AttachmentProperties {},
                    Content = "A List of all files"
                };
            }
        }*/
        await Context.Channel.SendMessageAsync(msg);
        return returnMsg;
    }
}
