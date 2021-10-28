namespace ViWatcher.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
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
            var duplicate = DbHelper.Single<Library>("lower(name) = lower(@1) and uid <> @2", library.Name, library.Uid);
            if (duplicate != null && duplicate.Uid != Guid.Empty)
                throw new Exception("Duplicate name.");

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

        [HttpDelete]
        public void Delete([FromBody] DeleteModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<Library>(model.Uids);
        }
    }
}