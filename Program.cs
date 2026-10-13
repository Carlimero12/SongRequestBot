using System;
using SongRequestBot;
using Microsoft.Extensions.Logging;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
namespace SongRequestBot
{

    // choose a god and start praying this works :3 .
public class Program()
{
    static Program()
    {
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            string expectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dlls", $"{assemblyName.Name}.dll");
            
            if (File.Exists(expectedPath))
            {
                return context.LoadFromAssemblyPath(expectedPath);
            }
            return null;
        };
    }
    public static bool DevMode = false;
    public static bool QueueEmpty = false;
    public static List<string> authUsers = ["carlimero12", "jorgenthehorg", "tehvessel"];
    static async Task Main()
    {
        GlobalExceptionHandler errorHandler = new GlobalExceptionHandler();
        AppDomain.CurrentDomain.UnhandledException += async (s, e) => await errorHandler.TryHandleAsync(null, (Exception)e.ExceptionObject, CancellationToken.None);
        TaskScheduler.UnobservedTaskException += async (s, e) => { await errorHandler.TryHandleAsync(null, e.Exception, CancellationToken.None); e.SetObserved(); };
        Console.WriteLine("Start");
        Thread StaticCaller = new(new ThreadStart(TwitchClientAPI.StaticMethod));
        StaticCaller.Start();
        Thread DiscordStatic = new(new ThreadStart(DiscordClientApi.DiscordClientInitializer));
        DiscordStatic.Start();
        await Task.Delay (Timeout.Infinite);
    }
}
}