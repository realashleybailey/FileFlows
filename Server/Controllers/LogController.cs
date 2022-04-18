namespace FileFlows.Server.Controllers
{
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
        /// <returns>The system log</returns>
        [HttpGet]
        public string Get()
        {
            if (Logger.Instance is Logger logger)
                return logger.GetTail(300);
            return String.Empty;
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
