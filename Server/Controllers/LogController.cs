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
            if (Logger.Instance is Logger logger)
            {
                string log = logger.GetTail(1000, logLevel);
                string html = LogToHtml.Convert(log);
                return html;
            }
            return string.Empty;
        }

        /// <summary>
        /// Downloads the full system log
        /// </summary>
        /// <returns>a download result of the full system log</returns>
        [HttpGet("download")]
        public IActionResult Download()
        {
            if (Logger.Instance is Logger logger)
            {
                byte[] content = System.IO.File.ReadAllBytes(logger.LogFile);
                return File(content, "application/octet-stream", "FileFlows.log");
            }
            
            string log = Logger.Instance.GetTail(10_000);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(log);
            return File(data, "application/octet-stream", "FileFlows.log");
        }
    }
}
