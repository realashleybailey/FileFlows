namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Workers;
    using Microsoft.AspNetCore.SignalR;
    using FileFlows.Server.Hubs;

    /// <summary>
    /// This controller will be responsible for knowing about the workers and the nodes
    /// When a worker starts, this needs to be informed, when its finished, it needs to be told too
    /// This needs to be able to kill a worker running on any node
    /// </summary>
    [Route("/api/worker")]
    public class WorkerController : Controller
    {
        private readonly static Dictionary<Guid, FlowExecutorInfo> Executors = new();
        private Queue<Guid> CompletedExecutors = new Queue<Guid>(50);

        private IHubContext<FlowHub> Context;

        public WorkerController(IHubContext<FlowHub> context)
        {
            this.Context = context;
        }

        private async Task<string> GetLogFileName(Guid libraryFileUid)
        {
            var settings = await new SettingsController().Get();
            string logFile = Path.Combine(settings.LoggingPath, libraryFileUid + ".log");
            return logFile;
        }

        [HttpPost("work/start")]
        public async Task<FlowExecutorInfo> StartWork([FromBody] FlowExecutorInfo info)
        {
            try
            {
                // try to delete a log file for this library file if one already exists (incase the flow was cancelled and now its being re-run)                
                string logFile = await GetLogFileName(info.LibraryFile.Uid);
                if (System.IO.File.Exists(logFile))
                    System.IO.File.Delete(logFile);
            }
            catch (Exception) { }

            info.Uid = Guid.NewGuid();
            info.LastUpdate = DateTime.UtcNow;
            Executors.Add(info.Uid, info);
            return info;
        }

        [HttpPost("work/finish")]
        public async void FinishWork([FromBody] FlowExecutorInfo info)
        {
            if (string.IsNullOrEmpty(info.Log) == false)
            {
                // this contains the full log file, save it incase a message was lost or recieved out of order during processing
                try
                {
                    string logfile = await GetLogFileName(info.LibraryFile.Uid);
                    System.IO.File.WriteAllText(logfile, info.Log);
                }
                catch (Exception) { }
            }
            lock (Executors)
            {
                CompletedExecutors.Append(info.Uid);
                if (Executors.ContainsKey(info.Uid))
                    Executors.Remove(info.Uid);
                else
                    Logger.Instance?.DLog("Could not remove as not in list of Executors: " + info.Uid);
            }
        }

        [HttpPost("work/update")]
        public void UpdateWork([FromBody] FlowExecutorInfo info)
        {
            if (info.LibraryFile != null)
            {
                var lfController = new LibraryFileController();
                var existing = lfController.Get(info.LibraryFile.Uid).Result;
                if (existing != null)
                {
                    bool recentUpdate = existing.DateModified > DateTime.UtcNow.AddSeconds(-10);
                    if ((existing.Status == FileStatus.ProcessingFailed && info.LibraryFile.Status == FileStatus.Processing && recentUpdate) == false)
                    {
                        existing = lfController.Update(info.LibraryFile).Result; // incase the status of the library file has changed
                        
                    }

                    if (existing.Status == FileStatus.ProcessingFailed || existing.Status == FileStatus.Processed)
                    {
                        lock (Executors)
                        {
                            CompletedExecutors.Append(info.Uid);
                            if (Executors.ContainsKey(info.Uid))
                                Executors.Remove(info.Uid);
                            return;
                        }
                    }
                }
            }

            info.LastUpdate = DateTime.UtcNow;
            lock (Executors)
            {
                if (CompletedExecutors.Contains(info.Uid))
                    return; // this call was delayed for some reason

                if (Executors.ContainsKey(info.Uid))
                    Executors[info.Uid] = info;
                //else // this is causing a finished executors to stick around.
                //    Executors.Add(info.Uid, info);
            }
        }

        [HttpPost("clear/{nodeUid}")]
        public async Task Clear([FromRoute] Guid nodeUid)
        {
            lock (Executors)
            {
                var toRemove = Executors.Where(x => x.Value.NodeUid == nodeUid).ToArray();
                foreach (var item in toRemove)
                    Executors.Remove(item.Key);
            }
            await new LibraryFileController().ResetProcessingStatus(nodeUid);
        }

        [HttpGet]
        public IEnumerable<FlowExecutorInfo> GetAll()
        {
            // we don't want to return the logs here, too big
            return Executors.Values.Select(x => new FlowExecutorInfo
            {
                // have to create a new object, otherwise if we chagne the log we change the log on the shared object
                LibraryFile = x.LibraryFile,
                CurrentPart = x.CurrentPart,
                CurrentPartName = x.CurrentPartName,
                CurrentPartPercent = x.CurrentPartPercent,
                Library = x.Library,
                NodeUid = x.NodeUid,
                NodeName = x.NodeName,
                RelativeFile = x.RelativeFile,
                StartedAt = x.StartedAt,
                TotalParts = x.TotalParts,
                Uid = x.Uid,
                WorkingFile = x.WorkingFile
            });
        }

        [HttpGet("{uid}/log")]
        public async Task<string> Log([FromRoute] Guid uid, [FromQuery] int lineCount = 0)
        {
            var settings = await new SettingsController().Get();
            string file = Path.Combine(settings.LoggingPath, uid + ".log");
            if (System.IO.File.Exists(file))
                return System.IO.File.ReadAllText(file);
            return String.Empty;
        }
        [HttpDelete("by-file/{uid}")]
        public async Task AbortByFile(Guid uid)
        {
            Guid executorId;
            lock (Executors)
            {
                executorId = Executors.Where(x => x.Value.LibraryFile.Uid == uid).Select(x => x.Key).FirstOrDefault();
            }
            if (executorId == Guid.Empty)
            {
                Logger.Instance?.WLog("Failed to locate Flow executor with library file: " + uid);
                foreach (var executor in Executors)
                    Logger.Instance?.ILog($"Flow Executor: {executor.Key} = {executor.Value?.LibraryFile?.Uid} = {executor.Value?.LibraryFile?.Name}");
                return;
            }
            await Abort(executorId);
        }

        [HttpDelete("{uid}")]
        public async Task Abort(Guid uid)
        {
            try
            {
                FlowExecutorInfo flowinfo;
                Executors.TryGetValue(uid, out flowinfo);

                Logger.Instance?.ILog("Sending AbortFlow " + uid);
                await this.Context.Clients.All.SendAsync("AbortFlow", uid);

                if (flowinfo?.LibraryFile != null)
                {
                    Logger.Instance?.ILog("Sending AbortFlow " + flowinfo.LibraryFile.Uid);
                    await this.Context.Clients.All.SendAsync("AbortFlow", flowinfo?.LibraryFile.Uid);
                    var libController = new LibraryFileController();
                    var libfile = await libController.Get(flowinfo.LibraryFile.Uid);
                    if (libfile.Status == FileStatus.Processing)
                    {
                        libfile.Status = FileStatus.ProcessingFailed;
                        Logger.Instance?.ILog("Library file setting status to failed: " + libfile.Status + " => " + libfile.RelativePath);
                        await libController.Update(libfile);
                    }
                    else
                    {
                        Logger.Instance?.ILog("Library file status doesnt need changing: " + libfile.Status + " => " + libfile.RelativePath);
                    }
                }

                _ = Task.Run(async () =>
                {
                    await Task.Delay(10_000);
                    lock (Executors)
                    {
                        if (Executors.TryGetValue(uid, out FlowExecutorInfo info))
                        {
                            if (info.LastUpdate < DateTime.UtcNow.AddMinutes(-1))
                            {
                                // its gone quiet, kill it
                                Executors.Remove(uid);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Error aborting flow: " + ex.Message);
            }
        }
    }
}