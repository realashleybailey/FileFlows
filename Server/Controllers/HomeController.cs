namespace FileFlows.Server.Controllers;

using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ResponseCache(NoStore = true, Duration = 0)]
    public IActionResult Index()
    {
        Logger.Instance.DLog("HomeController.Index");
        string index = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "wwwroot", "index.html");
        using var stream = System.IO.File.OpenRead(index);
        return File(stream, "text/html");
    }
}
