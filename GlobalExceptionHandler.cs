using Google;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext? httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is GoogleApiException gEx && (gEx.HttpStatusCode == HttpStatusCode.TooManyRequests || gEx.Error?.Message?.Contains("Quota") == true))
        {
            Console.WriteLine("Google API quota exhausted. Running handler logic...");
            return true;
        }

        Console.WriteLine($"Unhandled exception handled globally: {exception.Message}");
        System.Environment.Exit(0);
        return true;
    }
}
