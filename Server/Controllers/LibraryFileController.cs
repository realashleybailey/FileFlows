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
                           .ThenBy(x => x.DateCreated);
        }

        [HttpGet("upcoming")]
        public IEnumerable<LibraryFile> Upcoming([FromQuery] FileStatus? status)
        {
            return GetAll(FileStatus.Unprocessed).Take(10);
        }

        [HttpGet("recently-finished")]
        public IEnumerable<LibraryFile> RecentlyFinished([FromQuery] FileStatus? status)
        {
            return DbHelper.Select<LibraryFile>()
                           .Where(x => x.Status == FileStatus.Processed)
                           .OrderByDescending(x => x.ProcessingEnded)
                           .Take(10);
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
            var logFile = DbHelper.Single<Settings>()?.GetLogFile(uid);
            if (string.IsNullOrEmpty(logFile))
                return string.Empty;
            if (System.IO.File.Exists(logFile) == false)
                return string.Empty;


            try
            {
                Stream stream = System.IO.File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader streamReader = new StreamReader(stream);
                return streamReader.ReadToEnd();
            }
            catch (System.Exception ex)
            {
                return "Error opening log: " + ex.Message;
            }
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