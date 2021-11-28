namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/library-file")]
    public class LibraryFileController : Controller
    {
        [HttpGet]
        public IEnumerable<LibraryFile> GetAll([FromQuery] FileStatus? status)
        {
            // clear the log, its too big to send with this data
            var results = DbHelper.Select<LibraryFile>()
                                   .Where(x => status == null || x.Status == status.Value);

            if (status == FileStatus.Unprocessed)
                return results.OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                              .ThenBy(x => x.DateCreated);

            if (status == FileStatus.Processing)
                return results;

            return results.OrderByDescending(x => x.ProcessingEnded);
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

        [HttpPost("move-to-top")]
        public void MoveToTop([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete

            var list = model.Uids.ToList();

            var libFiles = DbHelper.Select<LibraryFile>()
                                   .Where(x => x.Status == FileStatus.Unprocessed)
                                   .OrderBy(x =>
                                    {
                                        int index = list.IndexOf(x.Uid);
                                        if (index >= 0)
                                        {
                                            x.Order = index + 1;
                                            return index;
                                        }
                                        else if (x.Order > 0)
                                        {
                                            x.Order = list.Count + x.Order - 1;
                                            return x.Order;
                                        }
                                        return int.MaxValue;
                                    })
                                    .Where(x => x.Order > 0)
                                    .ToList();
            int order = 0;
            foreach (var libFile in libFiles)
            {
                libFile.Order = ++order;
                DbHelper.Update(libFile);
            }
        }

        [HttpDelete]
        public void Delete([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<LibraryFile>(model.Uids);
        }
    }
}