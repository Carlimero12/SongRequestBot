namespace SongRequestBot
{

    // choose a god and start praying this works :3 .
    // Erebos, the god of darkness :D - NightTheFox
public class Program()
{
    public static bool DevMode = false;
    public static bool SubVipMode = false;
    public static bool QueueEmpty = false;
    public static List<string> premiumUsers = ["carlimero12"];
    public static List<string> authUsers = ["carlimero12", "jorgenthehorg", "tehvessel"];
    static async Task Main()
    {
        Console.WriteLine("Start");
        // Thread StaticCaller = new(new ThreadStart(TwitchClientAPI.StaticMethod));
        // StaticCaller.Start();
        Thread DiscordStatic = new(new ThreadStart(DiscordClientApi.DiscordClientInitializer));
        DiscordStatic.Start();
        await Task.Delay (Timeout.Infinite);
    }
}
}