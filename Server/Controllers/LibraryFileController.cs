namespace FileFlow.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Server.Helpers;
    using FileFlow.Shared.Models;

    [Route("/api/library-file")]
    public class LibraryFileController : Controller
    {
        [HttpGet]
        public IEnumerable<LibraryFile> GetAll([FromQuery] FileStatus? status)
        {
            // clear the log, its too big to send with this data
            return DbHelper.Select<LibraryFile>()
                           .Where(x => status == null || x.Status == status.Value)
                           .OrderBy(x => x.Order != -1 ? x.Order : int.MaxValue)
                           .ThenBy(x => x.DateCreated)
                           .Select(x => { x.Log = ""; return x; });
        }

        [HttpGet("upcoming")]
        public IEnumerable<LibraryFile> Upcoming([FromQuery] FileStatus? status)
        {
            return GetAll(FileStatus.Unprocessed).Take(5);
        }

        [HttpGet("recently-finished")]
        public IEnumerable<LibraryFile> RecentlyFinished([FromQuery] FileStatus? status)
        {
            return DbHelper.Select<LibraryFile>()
                           .Where(x => x.Status == FileStatus.Processed)
                           .OrderByDescending(x => x.ProcessingEnded)
                           .Take(5)
                           .Select(x => { x.Log = ""; return x; });
        }

        [HttpGet("status")]
        public IEnumerable<LibraryStatus> GetStatus()
        {
            return DbHelper.Select<LibraryFile>()
                           .GroupBy(x => x.Status)
                           .Select(x => new LibraryStatus { Status = x.Key, Count = x.Count() });

        }

        [HttpGet("{uid}")]
        public LibraryFile Get(Guid uid)
        {
            return DbHelper.Single<LibraryFile>(uid);
        }

        [HttpGet("{uid}/log")]
        public string GetLog(Guid uid)
        {
            return DbHelper.Single<LibraryFile>(uid)?.Log ?? "";
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