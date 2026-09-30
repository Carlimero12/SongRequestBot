namespace SongRequestBot;
using System;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Transactions;

public class ConfigFile()
{
    public required string token { get; set; }
    public required string username { get; set; }
    public required string ytPlaylist { get; set; }
    public required string StreamerName { get; set; }
    public required int maxSongs { get; set; }
}

public class TLIBSecrets
{
    static string dirpath = Directory.GetCurrentDirectory();
    static string confPath = Path.Combine(dirpath, "config.json");
    public static ConfigFile config()
    {
        ConfigFile conf = new ConfigFile
        {
            token = "",
            username = "",
            ytPlaylist = "",
            StreamerName = "",
            maxSongs = 10
        };
        if (!File.Exists(confPath))
        {   
            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };

            JsonSerializerOptions optionsCopy = new(options);
            string ConfigJson = JsonSerializer.Serialize(conf, optionsCopy);
            File.WriteAllText(confPath, ConfigJson);
            Console.WriteLine("\nPlease fill out the config file: config.Json first.\nPress any Key to close.");
            System.Environment.Exit(1);
            return conf;
        } else
        {
            string configFileContent = File.ReadAllText(confPath);
            ConfigFile tempconf = JsonSerializer.Deserialize<ConfigFile>(configFileContent);
            if (tempconf == null) 
            { File.Delete(confPath); System.Environment.Exit(1); }
            else if (tempconf.token == "" || tempconf.username == "" || tempconf.ytPlaylist == "" || tempconf.StreamerName == "")
            {
                System.Environment.Exit(1);
            } 
            else {
                conf = tempconf;
                return conf;
            }
            
        }
        return conf;
    }
}