namespace SongRequestBot
{
[System.Runtime.Versioning.UnsupportedOSPlatform("android")]
[System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    // choose a god and start praying this works :3 - Carlimero12
    // Erebos, the god of darkness :D - NightTheFox
    // Nephthys, the goddess of death, mourning, and night/darkness :3 -Carlimero12
public class Program()
{
    public static bool DevMode = false;
    public static bool SubVipMode = false;
    public static bool QueueEmpty = false;
    public static List<string> premiumUsers = ["carlimero12"];
    public static int HowManyOfAKind = 100;
    public static List<string> RandomIds = [
        "7jkFmkucGw0",
        "LMeluRz2wv4",
        "CgayKlnjmMg",
        "52i14wYBef8",
        "hHtv2XMZlKs",
        "LACbVhgtx9I",
    ];
    public static List<string> authUsers = ["carlimero12", "jorgenthehorg", "tehvessel"];
    static async Task Main()
    {
        Console.WriteLine("Start");

        if (File.Exists("config.json"))
        {
            ConfigGenerator.ConfigCLI();
            return;
        }         

        if (TLIBSecrets.config().token != "null")
        {
            TwitchClientAPI.StaticMethod();
        }

        if (TLIBSecrets.config().discordToken != "null")
        {
            DiscordClientApi.DiscordClientInitializer();
        }
            
        await Task.Delay (Timeout.Infinite);
    }
}
}