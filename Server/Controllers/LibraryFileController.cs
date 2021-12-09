namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/library-file")]
    public class LibraryFileController : ControllerStore<LibraryFile>
    {
        [HttpGet]
        public async Task<IEnumerable<LibraryFile>> GetAll([FromQuery] FileStatus? status, [FromQuery] int skip = 0, [FromQuery] int top = 0)
        {
            var libraryFiles = await base.GetDataList();

            if (status != null)
            {
                FileStatus searchStatus = status.Value == FileStatus.OutOfSchedule ? FileStatus.Unprocessed : status.Value;
                libraryFiles = libraryFiles.Where(x => x.Status == searchStatus).ToList();
            }

            var libraries = await new LibraryController().GetData();


            if (status == FileStatus.Unprocessed || status == FileStatus.OutOfSchedule)
            {
                return libraryFiles
                              .Where(x =>
                              {
                                  // unprocessed just show the enabled libraries
                                  if (x.Library == null || libraries.ContainsKey(x.Library.Uid) == false)
                                      return false;
                                  var lib = libraries[x.Library.Uid];
                                  if (lib.Enabled == false)
                                      return false;
                                  if (TimeHelper.InSchedule(lib.Schedule) == false)
                                      return status == FileStatus.OutOfSchedule;
                                  return status == FileStatus.Unprocessed;
                              })
                              .OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                              .ThenByDescending(x =>
                              {
                                  // check the processing priority of the library
                                  if (x.Library != null && libraries.ContainsKey(x.Library.Uid))
                                  {
                                      return (int)libraries[x.Library.Uid].Priority;
                                  }
                                  return (int)ProcessingPriority.Normal;
                              })
                              .ThenBy(x => x.DateCreated);
            }

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
            var libraryFiles = await GetDataList();
            return libraryFiles
                           .Where(x => x.Status == FileStatus.Processed)
                           .OrderByDescending(x => x.ProcessingEnded)
                           .Take(10);
        }

        [HttpGet("status")]
        public async Task<IEnumerable<LibraryStatus>> GetStatus()
        {
            var libraryFiles = await GetDataList();
            var libraries = await new LibraryController().GetData();
            var statuses = libraryFiles.Select(x =>
            {
                if (x.Status != FileStatus.Unprocessed)
                    return x.Status;
                // unprocessed just show the enabled libraries
                if (libraries.ContainsKey(x.Library.Uid) == false)
                    return (FileStatus) (-99);

                var lib = libraries[x.Library.Uid];
                if (lib.Enabled == false)
                    return (FileStatus)(-99);
                if (TimeHelper.InSchedule(lib.Schedule) == false)
                    return FileStatus.OutOfSchedule;
                return FileStatus.Unprocessed;
            });

            return statuses.Where(x => (int)x != -99).GroupBy(x => x)
                           .Select(x => new LibraryStatus { Status = x.Key, Count = x.Count() });

        }


        [HttpGet("{uid}")]
        public async Task<LibraryFile> Get(Guid uid)
        {
            return await GetByUid(uid);
        }

        [HttpGet("{uid}/log")]
        public async Task<string> GetLog(Guid uid)
        {
            var logFile = (await new SettingsController().Get())?.GetLogFile(uid);
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
            var libraryFiles = await GetDataList();

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
                await DbHelper.Update(libFile);
            }
        }

        internal async Task AddMany(LibraryFile[] libraryFiles)
        {
            await DbHelper.AddMany(libraryFiles);
            lock (Data)
            {
                foreach (var lf in libraryFiles)
                    Data.Add(lf.Uid, lf);
            }
        }

        [HttpDelete]
        public async Task Delete([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            await DeleteAll(model);
        }

        [HttpGet("shrinkage")]
        public async Task<ShrinkageData> Shrinkage()
        {
            double original = 0;
            double final = 0;

            var files = await GetDataList();
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

        internal async Task UpdateFlowName(Guid uid, string name)
        {
            var libraryFiles = await GetDataList();
            foreach (var lf in libraryFiles.Where(x => x.Flow?.Uid == uid))
            {
                lf.Flow.Name = name;
                await Update(lf);
            }
        }
    }
}