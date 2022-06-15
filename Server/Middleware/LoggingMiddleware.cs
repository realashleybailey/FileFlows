using FileFlows.Server.Controllers;

namespace FileFlows.Server.Middleware;

/// <summary>
/// A middleware used to log all requests
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructs a instance of the exception middleware
    /// </summary>
    /// <param name="next">the next middleware to call</param>
    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">the HttpContext executing this middleware</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        finally
        {
            try
            {
                var settings = new SettingsController().Get().Result;
                if (settings.LogEveryRequest)
                {
                    Logger.Instance.ILog(
                        $"REQUEST [{context.Request?.Method}] [{context.Response?.StatusCode}]: {context.Request?.Path.Value}");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
