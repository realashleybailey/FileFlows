namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;
    using global::Server.Helpers;

    [Route("/api/library-file")]
    public class LibraryFileController : Controller
    {
        [HttpGet]
        public async Task<IEnumerable<LibraryFile>> GetAll([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
        {
            DateTime start = DateTime.Now;
            var libraryFiles = await CacheStore.GetLibraryFiles();
            TimeSpan timeTaken = DateTime.Now - start;
            if (status != null)
                libraryFiles = libraryFiles.Where(x => x.Status == status.Value).ToList();

            HttpContext.Response.Headers.Add("x-time-taken", new Microsoft.Extensions.Primitives.StringValues(timeTaken.ToString()));

            if (status == FileStatus.Unprocessed)
                return libraryFiles.OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                              .ThenBy(x => x.DateCreated);

            if (status == FileStatus.Processing)
                return libraryFiles;

            IEnumerable<LibraryFile> results = libraryFiles.OrderByDescending(x => x.ProcessingEnded);

            if (skip > 0)
                results = results.Skip(skip);
            if(top > 0)
                results = results.Take(top);



            return results;
        }

        [HttpGet("upcoming")]
        public async Task<IEnumerable<LibraryFile>> Upcoming([FromQuery] FileStatus? status)
        {
            var libFiles = await GetAll(FileStatus.Unprocessed);
            return libFiles.Take(10);
        }

        [HttpGet("recently-finished")]
        public async Task<IEnumerable<LibraryFile>> RecentlyFinished([FromQuery] FileStatus? status)
        {
            var libraryFiles = await CacheStore.GetLibraryFiles();
            return libraryFiles
                           .Where(x => x.Status == FileStatus.Processed)
                           .OrderByDescending(x => x.ProcessingEnded)
                           .Take(10);
        }

        [HttpGet("status")]
        public async Task<IEnumerable<LibraryStatus>> GetStatus()
        {
            var libraryFiles = await CacheStore.GetLibraryFiles();
            return libraryFiles.GroupBy(x => x.Status)
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
        public async Task MoveToTop([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete

            var list = model.Uids.ToList();

            // clear the list to make sure its upt to date
            CacheStore.ClearLibraryFiles();
            var libraryFiles = await CacheStore.GetLibraryFiles();

            var libFiles = libraryFiles
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
            CacheStore.ClearLibraryFiles();
        }

        [HttpGet("shrinkage")]
        public async Task<ShrinkageData> Shrinkage()
        {
            double original = 0;
            double final = 0;

            var files = await CacheStore.GetLibraryFiles();
            foreach(var file in files)
            {
                if (file.Status != FileStatus.Processed || file.OriginalSize == 0 || file.FinalSize == 0)
                    continue;
                original += file.OriginalSize;
                final += file.FinalSize;
            }
            return new ShrinkageData
            {
                FinalSize = final,
                OriginalSize = original
            };
        }
    }
}