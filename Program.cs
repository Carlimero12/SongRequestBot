using System;
using SongRequestBot;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
namespace SongRequestBot
{
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

    public static ILoggerFactory loggerFactory =  LoggerFactory.Create(c => c
        .AddConsole()
    );
    public static bool DevMode = false;
    static async Task Main()
    {
        GlobalExceptionHandler errorHandler = new GlobalExceptionHandler();
        AppDomain.CurrentDomain.UnhandledException += async (s, e) => await errorHandler.TryHandleAsync(null, (Exception)e.ExceptionObject, CancellationToken.None);
        TaskScheduler.UnobservedTaskException += async (s, e) => { await errorHandler.TryHandleAsync(null, e.Exception, CancellationToken.None); e.SetObserved(); };
        Console.WriteLine("Start");
        Thread StaticCaller = new(new ThreadStart(TwitchClientAPI.StaticMethod));
        StaticCaller.Start();
        await Task.Delay(Timeout.Infinite);
    }
}
}