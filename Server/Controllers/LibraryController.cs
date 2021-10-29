namespace FileFlow.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Server.Helpers;
    using FileFlow.Shared.Models;

    [Route("/api/library")]
    public class LibraryController : Controller
    {
        [HttpGet]
        public IEnumerable<Library> GetAll()
        {
            return DbHelper.Select<Library>();
        }

        [HttpGet("{uid}")]
        public Library Get(Guid uid)
        {
            return DbHelper.Single<Library>(uid);
        }

        [HttpPost]
        public Library Save([FromBody] Library library)
        {
            var duplicate = DbHelper.Single<Library>("lower(name) = lower(@1) and uid <> @2", library.Name, library.Uid.ToString());
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