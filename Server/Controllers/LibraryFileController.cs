namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/library-file")]
    public class LibraryFileController : ControllerStore<LibraryFile>
    {

        /// <summary>
        /// Gets the next library file for processing, and puts it into progress
        /// </summary>
        /// <param name="nodeName">the nameof hte node processing the file</param>
        /// <param name="nodeUid">the Uid of the node processing the file </param>
        /// <param name="workerUid">the UId of the worker</param>
        /// <returns>the next library file to process</returns>
        [HttpGet("next-file")]
        public async Task<LibraryFile> GetNext([FromQuery] string nodeName, [FromQuery] Guid nodeUid, [FromQuery] Guid workerUid)
        {
            var data = (await GetAll(FileStatus.Unprocessed)).ToArray();
            _mutex.WaitOne();
            try
            {
                // iterate these incase, something starts processing
                for(int i = 0; i < data.Length; i++)
                {
                    if (data[i].Status == FileStatus.Unprocessed)
                    {
                        data[i].Status = FileStatus.Processing;
                        data[i].Node = new ObjectReference { Uid = nodeUid, Name = nodeName };
                        data[i].WorkerUid = workerUid;
                        data[i].ProcessingStarted = DateTime.UtcNow;
                        data[i] = await DbManager.Update(data[i]);
                        return data[i];
                    }
                }
                return null;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

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

        internal async Task ResetProcessingStatus(Guid nodeUid)
        {
            var libfiles = await GetDataList();
            var uids = libfiles.Where(x => x.Status == FileStatus.Processing && x.Node?.Uid == nodeUid).Select(x => x.Uid).ToArray();
            if (uids.Any())
                await Reprocess(new ReferenceModel { Uids = uids });
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


        [HttpPut]
        public async Task<LibraryFile> Update([FromBody] LibraryFile file)
        {
            var existing = await GetByUid(file.Uid);
            if (existing == null)
                throw new Exception("Not found");
            existing.Status = file.Status;
            existing.Node = file.Node;
            existing.FinalSize = file.FinalSize;
            if(file.OriginalSize > 0)
                existing.OriginalSize = file.OriginalSize;
            existing.Flow = file.Flow;
            existing.ProcessingEnded = file.ProcessingEnded;
            existing.ProcessingStarted = file.ProcessingStarted;
            existing.WorkerUid = file.WorkerUid;
            return await base.Update(existing);
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

        [HttpPost("reprocess")]
        public async Task Reprocess([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            var list = model.Uids.ToList();

            // clear the list to make sure its upt to date
            var libraryFiles = await GetData();
            foreach (var uid in model.Uids)
            {
                LibraryFile item;
                lock (libraryFiles)
                {
                    if (libraryFiles.ContainsKey(uid) == false)
                        continue;
                    item = libraryFiles[uid];
                    if (item.Status != FileStatus.ProcessingFailed && item.Status != FileStatus.Processed)
                        continue;
                    item.Status = FileStatus.Unprocessed;
                }
                await Update(item);
            }
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

        [HttpGet("shrinkage-groups")]
        public async Task<Dictionary<string, ShrinkageData>> ShrinkageGroups()
        {
            var files = await GetDataList();
            Dictionary<string, ShrinkageData> libraries = new ();
            ShrinkageData total = new ShrinkageData();
            foreach (var file in files)
            {
                if (file.Status != FileStatus.Processed || file.OriginalSize == 0 || file.FinalSize == 0)
                    continue;
                total.FinalSize += file.FinalSize;
                total.OriginalSize += file.OriginalSize;
                total.Items++;
                if (libraries.ContainsKey(file.Library.Name) == false)
                {
                    libraries.Add(file.Library.Name, new ShrinkageData()
                    {
                        FinalSize = file.FinalSize,
                        OriginalSize = file.OriginalSize
                    });
                }
                else
                {

                    libraries[file.Library.Name].OriginalSize += file.OriginalSize;
                    libraries[file.Library.Name].FinalSize += file.FinalSize;
                    libraries[file.Library.Name].Items++;
                }
            }

            if (libraries.Count > 5)
            {
                ShrinkageData other = new ShrinkageData();
                while (libraries.Count > 4)
                {
                    List<string> toRemove = new();
                    var sd = libraries.OrderBy(x => x.Value.Items).First();
                    other.Items += sd.Value.Items;
                    other.FinalSize += sd.Value.FinalSize;
                    other.OriginalSize += sd.Value.OriginalSize;
                    libraries.Remove(sd.Key);
                }
                libraries.Add("###OTHER###", other);
            }
#if (DEBUG)
            if (libraries.Any() == false)
            {
                Random rand = new Random(DateTime.Now.Millisecond);
                double min = 10_000_000;
                int count = 0;
                libraries = Enumerable.Range(1, 6).Select(x => new ShrinkageData
                {
                    FinalSize = rand.NextDouble() * min + min,
                    OriginalSize = rand.NextDouble() * min + min
                }).ToDictionary(x => "Library " + (++count), x => x);
                total.FinalSize = 0;
                total.OriginalSize = 0;
                foreach (var lib in libraries)
                {
                    total.FinalSize += lib.Value.FinalSize;
                    total.OriginalSize += lib.Value.OriginalSize;
                }
            }
#endif
            if(libraries.ContainsKey("###TOTAL###") == false) // so unlikely, only if they named a library this, but just incase they did
                libraries.Add("###TOTAL###", total);
            return libraries;
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

#if (DEBUG)
        [HttpGet("stream")]
        public async Task<IActionResult> StreamFile([FromQuery] string file)
        {
            // this method is testing if streaming a file to a node is doable
            // instead of having to setup mappings
            // ffmpeg handles it, but not every node would. so maybe have to bake in to the input node somehow...
            // but doing that VideoInput can stream the file to get the info, thats a lot of data to stream over
            // then its still on server, then when VideoEncode executes, we have to read the entire file again
            // thats less than ideal... so maybe just delete this code...
            return File(System.IO.File.OpenRead(file), "applicaiton/octet-stream");
        }
#endif
    }
}