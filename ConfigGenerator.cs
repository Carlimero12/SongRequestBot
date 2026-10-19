using System;
using System.Text.RegularExpressions;
using NetCord.JsonConverters;
namespace SongRequestBot;

public class ConfigGenerator
{

    public static string Input(string textQuestion)
    {
        Console.Clear();
        Console.WriteLine(textQuestion);
        string line;
        if ((line = Console.ReadLine()) != null)
        {
            return line;
        }
        Console.Clear();
        Console.WriteLine("There has been a Error with the Setup. Please restart the Application");
        Console.ReadLine();
        Environment.Exit(123);
        return "fb";
    }
    public static void ConfigCLI()
    {
        ConfigFile Configuration = new ConfigFile
        {
            StreamerName = "fb",
            username = "fb",
            ytPlaylist = "fb",
            maxSongs = -1,
            token = "fb",
            discordToken = "fb",
        };
        // The config tool


        string inputS;
        if ((inputS = Input("Please enter ur Twitch.tv name here: ")) != "fb")
        {
            Configuration.StreamerName = inputS.ToLower();
        } 
        
        if ((inputS = Input("Max amount of playlist lenght? (For no limit type -1): ")) != "fb")
        {
            try
            {
                Configuration.maxSongs = Int32.Parse(inputS);
            }
            catch (Exception e)
            {
                Console.Clear();
                Console.WriteLine(e.Message);
                Console.WriteLine("There has been a Error with the Setup. Please restart the Application");
                Console.ReadLine();
                Environment.Exit(123);
            }
        }

        inputS = Input("Input the Youtube Playlist link: ");
        string Link = inputS;
        Regex YoutubeVideoIDRegex = new Regex(@"(?:https?:\/\/)?(?:youtu\.be\/|(?:www\.|m\.)?youtube\.com\/(?:playlist|list|embed)(?:\.php)?(?:\?.*list=|\/))([a-zA-Z0-9\-_]+)", RegexOptions.IgnoreCase); //Insert Regex script here
        Match youtubeMatchID = YoutubeVideoIDRegex.Match(Link);
        if (youtubeMatchID.Groups[1].Value.Count() != 13)
        {
            Console.Clear();
            Console.WriteLine("There has been a Error with the Setup. Please restart the Application");
            Console.ReadLine();
            Environment.Exit(123);
        }
            Console.WriteLine(youtubeMatchID.Groups[1].Value);
        Configuration.ytPlaylist = youtubeMatchID.Groups[1].Value;

        inputS = Input("Enter the exact Username for ur Twitch Bot account.: ");
        Configuration.username = inputS.ToLower();

        inputS = Input("Enter the Discord bot Token (name this null to disable Discord side): ");
        Configuration.discordToken = inputS.ToLower();

        inputS = Input("Enter the Twitch Bot token (name this null to disable twitch side): ");

        TLIBSecrets.WriteToConfig(Configuration);
        
        Console.Clear();
        Console.WriteLine("Setup is done"+ System.Environment.NewLine + "Now restart this app to  start Bot" + System.Environment.NewLine + "Press any Key to close...");
        Console.ReadLine();
        Environment.Exit(123);
        }
}
