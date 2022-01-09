namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class HomeController : Controller
    {
        [ApiExplorerSettings(IgnoreApi = true)]
        [ResponseCache(NoStore = true, Duration = 0)]
        public IActionResult Spa()
        {
            return File("~/index.html", "text/html");
        }
    }
}