using FileFlows.Plugin;
using FileFlows.Server.Database;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// System log controller
/// </summary>
[Route("/api/log")]
public class LogController : Controller
{
    /// <summary>
    /// Gets the system log
    /// </summary>
    /// <returns>the system log</returns>
    [HttpGet]
    public string Get([FromQuery] Plugin.LogType logLevel = Plugin.LogType.Info)
    {
        if (Logger.Instance.TryGetLogger(out FileLogger logger))
        {
            string log = logger.GetTail(1000, logLevel);
            string html = LogToHtml.Convert(log);
            return html;
        }
        return string.Empty;
    }

    /// <summary>
    /// Searches the log using the given filter
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the messages found in the log</returns>
    [HttpPost("search")]
    public async Task<string> Search([FromBody] LogSearchModel filter)
    {
        if (DbHelper.UseMemoryCache)
            return "Not using external database, cannot search";
        var messages = await DbHelper.SearchLog(filter);
        string log = string.Join("\n", messages.Select(x =>
        {
            string prefix = x.Type switch
            {
                LogType.Info => "INFO",
                LogType.Error => "ERRR",
                LogType.Warning => "WARN",
                LogType.Debug => "DBUG",
                _ => ""
            };

            return x.LogDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + prefix + "] -> " + x.Message;
        }));
        string html = LogToHtml.Convert(log);
        return html;
    }

    /// <summary>
    /// Downloads the full system log
    /// </summary>
    /// <returns>a download result of the full system log</returns>
    [HttpGet("download")]
    public IActionResult Download()
    {
        if (Logger.Instance.TryGetLogger(out FileLogger logger))
        {
            string filename = logger.GetLogFilename();
            byte[] content = System.IO.File.ReadAllBytes(filename);
            
            return File(content, "application/octet-stream", new FileInfo(filename).Name);
        }
        
        string log = Logger.Instance.GetTail(10_000);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
        return File(data, "application/octet-stream", "FileFlows.log");
    }

    private readonly Dictionary<string, Guid> ClientUids = new (); 
        

    /// <summary>
    /// Logs a message to the server
    /// </summary>
    /// <param name="message">The log message to log</param>
    [HttpPost("message")]
    public async Task Log([FromBody] LogServiceMessage message)
    {
        if (message == null)
            return;
        if (string.IsNullOrEmpty(message.NodeAddress))
            return;

        if(ClientUids.TryGetValue(message.NodeAddress.ToLower(), out Guid clientUid) == false)
        {
            foreach (var node in new NodeController().GetAll().Result)
            {
                if (node.Address.ToLower() == message.NodeAddress.ToLower())
                    clientUid = node.Uid;
                if (string.IsNullOrEmpty(node.Address) == false &&
                    ClientUids.ContainsKey(node.Address.ToLower()) == false)
                {
                    ClientUids.Add(node.Address.ToLower(), node.Uid);
                }
            }
        }

        if (clientUid == Guid.Empty)
        {
            Logger.Instance.ILog($"Failed to find client '{message.NodeAddress}', could not log message");
            return;
        }

        if (Logger.Instance.TryGetLogger(out DatabaseLogger logger))
        {
            await logger.Log(clientUid, message.Type, message.Arguments);
        }
    }
}
