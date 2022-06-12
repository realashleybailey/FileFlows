using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Controllers
{
    using FileFlows.Shared.Helpers;
    using Microsoft.AspNetCore.Mvc;

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
            if (Logger.Instance.TryGetLogger(out FileLog logger))
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
            if (string.IsNullOrEmpty(filter.Message) && filter.ClientUid == null && filter.Type != LogType.Warning && filter.Type != LogType.Error)
                return Get(filter.Type ?? LogType.Info);
            
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
            if (Logger.Instance.TryGetLogger(out FileLog logger))
            {
                string filename = logger.GetLogFilename();
                byte[] content = System.IO.File.ReadAllBytes(filename);
                
                return File(content, "application/octet-stream", new FileInfo(filename).Name);
            }
            
            string log = Logger.Instance.GetTail(10_000);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
            return File(data, "application/octet-stream", "FileFlows.log");
        }
    }
}
