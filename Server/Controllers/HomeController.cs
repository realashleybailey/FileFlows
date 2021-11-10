namespace FileFlow.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class HomeController : Controller
    {
        public IActionResult Spa() => File("~/index.html", "text/html");
    }
}