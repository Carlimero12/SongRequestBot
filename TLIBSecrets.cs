namespace SongRequestBot;
using System;
using System.Text.Json;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Threading.Channels;
using NetCord.Rest;

public class ConfigFile()
{
    public required string token { get; set; }
    public required string username { get; set; }
    public required string ytPlaylist { get; set; }
    public required string StreamerName { get; set; }
    public required int maxSongs { get; set; }
    public required string discordToken { get; set; }
    public List<string> denyList { get; set; } = [];
    public int maxSongLenght { get; set; } = -1;
    public ulong CommandChannel { get; set; } = 1564658081231540274;
    public int MaxVidLenghtSec { get; set; } = 5*60;
    public string StartupMSG { get; set; } = "SongRequests";
    public string YTPlaylist_PrivacyStatus {get; set;} = "public";
}

public class TLIBSecrets
{
    static string dirpath = AppDomain.CurrentDomain.BaseDirectory;
    static string confPath = Path.Combine(dirpath, "config.json");

    public static void WriteToConfig(ConfigFile conf)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };
        string ConfigJson = JsonSerializer.Serialize(conf, options);
        File.WriteAllText(confPath, ConfigJson);
    }

    public static ConfigFile config()
    {
        ConfigFile conf = new ConfigFile
        {
            token = "[Your BotAccount token here]",
            username = "[The name of the botaccount]",
            ytPlaylist = "[The Id of the YT playlist u want Songs to be transported too]",
            StreamerName = "[The Streamer name the bot should be serving. All lowercase]",
            discordToken = "[The discord bots Tokens]",
            maxSongs = -1
        };
        if (!File.Exists(confPath))
        {   
            ConfigGenerator.ConfigCLI();
            return conf;
        } else
        {
            string configFileContent = File.ReadAllText(confPath);
            ConfigFile tempconf = JsonSerializer.Deserialize<ConfigFile>(configFileContent);
            if (tempconf == null) 
            { File.Delete(confPath); System.Environment.Exit(1); }
            else if (tempconf.token.Contains("[") || tempconf.username.Contains("[") || tempconf.ytPlaylist.Contains("[") || tempconf.StreamerName.Contains("[") || tempconf.discordToken.Contains("[") )
            {
                ConfigGenerator.ConfigCLI();
            } 
            else {
                conf = tempconf;
                return conf;
            }
            
        }
        return conf;
    }
    [Obsolete("This function is currently unpractical and should either be fixed or deleted.")]
    public static async Task OverwriteConf(ConfigFile newData)
    {
        PropertyInfo[] newPropInfo = newData.GetType().GetProperties();
        ConfigFile oldData = config();
        PropertyInfo[] oldPropInfo = oldData.GetType().GetProperties();
        int i = 0;
        foreach (PropertyInfo propInfo in newPropInfo)
        {
            i++;
            bool readOnly = propInfo.CanRead && !propInfo.CanWrite;
            bool writeOnly = propInfo.CanWrite && !propInfo.CanRead;
            bool readAndWrite = propInfo.CanWrite && propInfo.CanRead;

            bool confirm=false;
            if (oldPropInfo.GetValue(i).Equals(propInfo.GetValue(typeof(ConfigFile))))
            {
                continue;
            }
            else if (confirm = ConfigGenerator.Input($"The Configuration values of {propInfo.Name} dont match between new and old. Do you want to overwrite all of it? y/n").ToLower() == "y")
            {
                oldPropInfo = newPropInfo;
                break;
            }
            else
            {
                continue;
            }
        }

    }
}
