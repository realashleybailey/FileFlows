namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Route("/api/log")]
    public class LogController : Controller
    {
        [HttpGet]
        public string Get()
        {
            if (Logger.Instance is Logger logger)
                return logger.GetTail();
            return String.Empty;
        }
    }
}
