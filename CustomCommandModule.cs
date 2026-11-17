using NetCord;
using NetCord.Services;
using NetCord.Services.Commands;
using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Gateway;
using Google.Apis.YouTube.v3.Data;
using System.IO;
using System.Diagnostics;
using YoutubeExplode.Common;

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
    [SlashCommand("sr", "Adds a song with link to the Queue!",
        DefaultGuildPermissions = Permissions.SendMessages,
        Contexts = [InteractionContextType.Guild])]
    public async Task SR([SlashCommandParameter(Name = "link", Description = "The Link to the YT Vid")] string @link)
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage()
        );
        if (Context.Channel.Id != TLIBSecrets.config().CommandChannel && !Program.DevMode && Context.Guild?.Id == 1294646380714917959)
        {
            await Context.Interaction.DeleteResponseAsync();
            return;
        }

        User user = Context.User;
        ulong ChannelId = Context.Channel.Id;
        if (link == null)
        {
            await Context.Interaction.ModifyResponseAsync(message => 
                message.Content = "The Link Parameter must not be Null"
            );
            return;
        }
        Task<string> s = Task.Run(async () => await DiscordClientApi.AddSong_Command(link, user, ChannelId));
        await Context.Interaction.ModifyResponseAsync(message => 
            message.Content = s.Result
        );
        return;
    }

    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("srname", "Adds a song with Name to the Queue!",
        DefaultGuildPermissions = Permissions.SendMessages,
        Contexts = [InteractionContextType.Guild])]
    public async Task srname([SlashCommandParameter(Name = "songname", Description = "The Name of the YT Vid (maybe with artist name)")] string @link)
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage()
        );
        if (Context.Channel.Id != TLIBSecrets.config().CommandChannel && !Program.DevMode && Context.Guild?.Id == 1294646380714917959)
        {
            await Context.Interaction.DeleteResponseAsync();
            return;
        }

        User user = Context.User;
        ulong ChannelId = Context.Channel.Id;
        if (string.IsNullOrWhiteSpace(link))
        {
            await Context.Interaction.ModifyResponseAsync(message => 
                message.Content = "The Name Parameter must not be Empty"
            );
            return;
        }
        var results = await Program.youtubeEXCLI.Search.GetVideosAsync(link).CollectAsync(1);
        var video = results[0];
        string name1 = await YTPlaylistManagment.AddToPlaylist(video.Id.Value); 
        Console.WriteLine($"{video.Id.Value} -> {video.Title} # {video.Author}");
        await Context.Interaction.ModifyResponseAsync(message => 
            message.Content = $"Added {video.Title} by {video.Author} to queue."
        );
        return;
    }




    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("requeststatus", "Sets the Requests to open or Closed",
        DefaultGuildPermissions = Permissions.Administrator,
        Contexts = [InteractionContextType.Guild])]
    public string toggleRequests(SongRequestState state = SongRequestState.Check)
    {
        if (Context.Channel.Id != TLIBSecrets.config().CommandChannel)
        {
            return "";
        }
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
        if (Context.Channel.Id != TLIBSecrets.config().CommandChannel)
        {
            return "";
        }
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

    [RequireContext<ApplicationCommandContext>(RequiredContext.Guild)]
    [SlashCommand("resetplaylist", "Deletes and recreates the youtubePlaylist linked to yt api!",
        DefaultGuildPermissions = Permissions.Administrator,
        Contexts = [InteractionContextType.Guild])]
    public async Task resetplaylist()
    {
        Console.WriteLine(TLIBSecrets.config().ytPlaylist);
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage()
        );
        if (Context.Channel.Id != TLIBSecrets.config().CommandChannel && !Program.DevMode2 && Context.Guild?.Id == 1294646380714917959)
        {
            await Context.Interaction.DeleteResponseAsync();
            return;
        }

        await YTPlaylistManagment.youTubeService.Playlists.Delete(TLIBSecrets.config().ytPlaylist).ExecuteAsync();
        Playlist newPlaylist = new Playlist();
        newPlaylist.Snippet = new PlaylistSnippet();
        newPlaylist.Snippet.Title = $"{TLIBSecrets.config().StreamerName}'s Playlist";
        newPlaylist.Snippet.Description = $"The Songrequest Playlist for {TLIBSecrets.config().StreamerName}";
        newPlaylist.Status = new PlaylistStatus();
        newPlaylist.Status.PrivacyStatus = TLIBSecrets.config().YTPlaylist_PrivacyStatus;
        newPlaylist = await YTPlaylistManagment.youTubeService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();

        ConfigFile confs = TLIBSecrets.config();
        confs.ytPlaylist = newPlaylist.Id;
        TLIBSecrets.WriteToConfig(conf:confs);

        await Context.Interaction.ModifyResponseAsync(message => 
            message.Content = $"Successfully reset Playlist. New Playlist at: https://www.youtube.com/playlist?list={newPlaylist.Id}"
        );
    }
}
