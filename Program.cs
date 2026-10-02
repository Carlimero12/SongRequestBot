
using System;
using SongRequestBot;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace SongRequestBot
{
public class Program()
{
    public static ILoggerFactory loggerFactory =  LoggerFactory.Create(c => c
        .AddConsole()
    );
    static void Main()
    {
        GlobalExceptionHandler errorHandler = new GlobalExceptionHandler();
        AppDomain.CurrentDomain.UnhandledException += async (s, e) => await errorHandler.TryHandleAsync(null, (Exception)e.ExceptionObject, CancellationToken.None);
        TaskScheduler.UnobservedTaskException += async (s, e) => { await errorHandler.TryHandleAsync(null, e.Exception, CancellationToken.None); e.SetObserved(); };
        Console.WriteLine("Start");
        YTPlaylistManagment yTPlaylistManagment = new();
        TwitchClientAPI twitchClientAPI = new();

    }
}
}