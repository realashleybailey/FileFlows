namespace ViWatcher.Server.Controllers
{
    using System.ComponentModel;
    using System.Dynamic;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc;
    using ViWatcher.Plugins.Attributes;
    using ViWatcher.Server.Helpers;
    using ViWatcher.Shared.Models;

    [Route("/api/library")]
    public class LibraryController : Controller
    {
        [HttpGet]
        public IEnumerable<Library> GetAll()
        {
            return DbHelper.Select<Library>();
        }

        [HttpPost]
        public Library Save([FromBody] Library library)
        {
            return DbHelper.Update(library);
        }

        [HttpPut("state/{uid}")]
        public Library SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            var library = DbHelper.Single<Library>(uid);
            if (library == null)
                throw new Exception("Library not found.");
            if (enable != null)
            {
                library.Enabled = enable.Value;
                DbHelper.Update(library);
            }
            return library;
        }
    }
}