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
        private readonly static Dictionary<Guid, FlowExecutorInfo> Executors = new ();
        private static Mutex mutex = new();

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

            mutex.WaitOne();
            try
            {
                info.Uid = Guid.NewGuid();
                Executors.Add(info.Uid, info);
                return info;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        [HttpPost("work/finish")]
        public async void FinishWork([FromBody] FlowExecutorInfo info)
        {
            mutex.WaitOne();
            try
            {
                if(string.IsNullOrEmpty(info.Log) == false)
                {
                    // this contains the full log file, save it incase a message was lost or recieved out of order during processing
                    try
                    {
                        string logfile = await GetLogFileName(info.LibraryFile.Uid);
                        System.IO.File.WriteAllText(logfile, info.Log);
                    }
                    catch (Exception) { }  
                }
                if (Executors.ContainsKey(info.Uid))
                    Executors.Remove(info.Uid);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        [HttpPost("work/update")]
        public void UpdateWork([FromBody] FlowExecutorInfo info)
        {
            mutex.WaitOne();
            try
            {
                if (info.LibraryFile != null)
                    new LibraryFileController().Update(info.LibraryFile).Wait(); // incase the status of the library file has changed
                if (Executors.ContainsKey(info.Uid))
                    Executors[info.Uid] = info;
                else
                    Executors.Add(info.Uid, info);

            }
            finally
            {
                mutex.ReleaseMutex();
            }
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
            if(System.IO.File.Exists(file))
                return System.IO.File.ReadAllText(file);
            return String.Empty;
        }

        [HttpDelete("{uid}")]
        public async Task Abort(Guid uid)
        {
            await this.Context.Clients.All.SendAsync("AbortFlow", uid);
        }
    }
}