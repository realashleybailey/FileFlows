namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/library")]
    public class LibraryController : Controller
    {
        [HttpGet]
        public IEnumerable<Library> GetAll()
        {
            if (Globals.Demo)
                return Enumerable.Range(1, 5).Select(x => new Library
                {
                    Name = "Demo Library " + x,
                    Uid = Guid.NewGuid(),
                    Enabled = x < 3,
                    Filter = "\\.(mp4|mkv|avi|mpg|mov)$"
                });
            return DbHelper.Select<Library>();
        }

        [HttpGet("{uid}")]
        public Library Get(Guid uid)
        {
            if (Globals.Demo)
                return new Library
                {
                    Uid = uid,
                    Filter = @"\.(mp4|mkv|avi|mpg|mov)$",
                    Name = "Demo Library",
                    Enabled = true,
                    Flow = new ObjectReference
                    {
                        Uid = Guid.Empty,
                        Name = "Demo Flow"
                    }
                };
            return DbHelper.Single<Library>(uid);
        }

        [HttpPost]
        public Library Save([FromBody] Library library)
        {
            if (Globals.Demo)
                return library;

            var duplicate = DbHelper.Single<Library>("lower(name) = lower(@1) and uid <> @2", library.Name, library.Uid.ToString());
            if (duplicate != null && duplicate.Uid != Guid.Empty)
                throw new Exception("ErrorMessages.NameInUse");

            return DbHelper.Update(library);
        }

        [HttpPut("state/{uid}")]
        public Library SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            if(Globals.Demo)
                return new Library { Name = "Demo Library", Uid = uid, Enabled = enable == true };
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
        public void Delete([FromBody] ReferenceModel model)
        {
            if (Globals.Demo)
                return;
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<Library>(model.Uids);
        }
    }
}