namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class HomeController : Controller
    {
        [ResponseCache(NoStore = true, Duration = 0)]
        public IActionResult Spa() => File("~/index.html", "text/html");
    }
}