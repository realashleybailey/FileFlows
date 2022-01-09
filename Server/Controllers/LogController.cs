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
                return logger.GetTail();
            return String.Empty;
        }
    }
}
