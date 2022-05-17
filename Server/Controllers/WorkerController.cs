using FileFlows.Server.Helpers;

namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Workers;
    using Microsoft.AspNetCore.SignalR;
    using FileFlows.Server.Hubs;
    using FileFlows.Shared;

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

        /// <summary>
        /// Start work, tells the server work has started on a flow runner
        /// </summary>
        /// <param name="info">The info about the work starting</param>
        /// <returns>the updated info</returns>
        [HttpPost("work/start")]
        public FlowExecutorInfo StartWork([FromBody] FlowExecutorInfo info)
        {
            try
            {
                // try to delete a log file for this library file if one already exists (in case the flow was cancelled and now its being re-run)                
                LibraryFileLogHelper.DeleteLogs(info.LibraryFile.Uid);
            }
            catch (Exception) { }

            info.LibraryFile?.ExecutedNodes?.Clear();
            info.Uid = Guid.NewGuid();
            info.LastUpdate = DateTime.Now;
            Executors.Add(info.Uid, info);
            return info;
        }

        /// <summary>
        /// Finish work, tells the server work has finished on a flow runner
        /// </summary>
        /// <param name="info">Info about the finished work</param>
        [HttpPost("work/finish")]
        public async void FinishWork([FromBody] FlowExecutorInfo info)
        {
            if (string.IsNullOrEmpty(info.Log) == false)
            {
                // this contains the full log file, save it incase a message was lost or recieved out of order during processing
                try
                {
                    _ = LibraryFileLogHelper.SaveLog(info.LibraryFile.Uid, info.Log, saveHtml: true);
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

            if (info.LibraryFile != null)
            {
                var libfileController = new LibraryFileController();
                var libfile = await libfileController.Get(info.LibraryFile.Uid);
                if (libfile != null)
                {
                    info.LibraryFile.OutputPath = info.LibraryFile.OutputPath?.EmptyAsNull() ?? libfile.OutputPath;
                    Logger.Instance.ILog($"Recording final size for '{info.LibraryFile.FinalSize}' for '{info.LibraryFile.Name}'");
                    if(info.LibraryFile.FinalSize > 0)
                        libfile.FinalSize = info.LibraryFile.FinalSize;

                    libfile.OutputPath = info.LibraryFile.OutputPath;
                    libfile.Fingerprint = info.LibraryFile.Fingerprint;
                    libfile.ExecutedNodes = info.LibraryFile.ExecutedNodes ?? new List<ExecutedNode>();
                    await libfileController.Update(libfile);
                }
                _ = new StatisticsController().RecordStatistics(info.LibraryFile);
            }
        }

        /// <summary>
        /// Update work, tells the server about updated work on a flow runner
        /// </summary>
        /// <param name="info">The updated work information</param>
        [HttpPost("work/update")]
        public void UpdateWork([FromBody] FlowExecutorInfo info)
        {
            if (info.LibraryFile != null)
            {
                var lfController = new LibraryFileController();
                var existing = lfController.Get(info.LibraryFile.Uid).Result;
                if (existing != null)
                {
                    bool recentUpdate = existing.DateModified > DateTime.Now.AddSeconds(-10);
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

            info.LastUpdate = DateTime.Now;
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

        /// <summary>
        /// Clear all workers from a node.  Intended for clean up in case a node restarts.  
        /// This is called when a node first starts.
        /// </summary>
        /// <param name="nodeUid">The UID of the processing node</param>
        /// <returns>an awaited task</returns>
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

        /// <summary>
        /// Get all running flow executors
        /// </summary>
        /// <returns>A list of all running flow executors</returns>
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

        /// <summary>
        /// Gets the log of a library file
        /// </summary>
        /// <param name="uid">The UID of the library file</param>
        /// <param name="lineCount">The number of lines to fetch, 0 to fetch them all</param>
        /// <returns>The log of a library file</returns>
        [HttpGet("{uid}/log")]
        public string Log([FromRoute] Guid uid, [FromQuery] int lineCount = 0) => LibraryFileLogHelper.GetLog(uid);

        /// <summary>
        /// Abort work by library file
        /// </summary>
        /// <param name="uid">The UID of the library file to abort</param>
        /// <returns>An awaited task</returns>
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
                // may not have an executor, just update the status
                var libfileController = new LibraryFileController();
                var libFile = await libfileController.Get(uid);
                if (libFile is { Status: FileStatus.Processing })
                {
                    libFile.Status = FileStatus.ProcessingFailed;
                    await libfileController.Update(libFile);
                }
                return;
            }
            await Abort(executorId, uid);
        }

        /// <summary>
        /// Abort work 
        /// </summary>
        /// <param name="uid">The UID of the executor</param>
        /// <param name="libraryFileUid">the UID of the library file</param>
        /// <returns>an awaited task</returns>
        [HttpDelete("{uid}")]
        public async Task Abort([FromRoute] Guid uid, [FromQuery] Guid libraryFileUid)
        {
            try
            {
                FlowExecutorInfo? flowinfo;
                Executors.TryGetValue(uid, out flowinfo);
                if(flowinfo == null)
                {
                    flowinfo = Executors.Values.Where(x => x.LibraryFile?.Uid == uid || x.Uid == uid || x.NodeUid == uid).FirstOrDefault();
                }
                if(flowinfo == null)
                {
                    Logger.Instance?.WLog("Unable to find executor matching: " + uid);
                }

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
                
                await Task.Delay(6_000);
                lock (Executors)
                {
                    if (Executors.TryGetValue(uid, out FlowExecutorInfo? info))
                    {
                        if (info == null || info.LastUpdate < DateTime.Now.AddMinutes(-1))
                        {
                            // its gone quiet, kill it
                            Executors.Remove(uid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Error aborting flow: " + ex.Message);
            }
        }
    }
}