namespace FileFlow.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Server.Helpers;
    using FileFlow.Shared.Models;

    [Route("/api/library-file")]
    public class LibraryFileController : Controller
    {
        [HttpGet]
        public IEnumerable<LibraryFile> GetAll()
        {
            // clear the log, its too big to send with this data
            return DbHelper.Select<LibraryFile>().Select(x => { x.Log = ""; return x; });
        }

        [HttpGet("{uid}")]
        public LibraryFile Get(Guid uid)
        {
            return DbHelper.Single<LibraryFile>(uid);
        }
        [HttpDelete]
        public void Delete([FromBody] DeleteModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<LibraryFile>(model.Uids);
        }
    }
}