namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Workers;

    /// <summary>
    /// This controller will be responsible for knowing about the workers and the nodes
    /// When a worker starts, this needs to be informed, when its finished, it needs to be told too
    /// This needs to be able to kill a worker running on any node
    /// </summary>
    [Route("/api/worker")]
    public class WorkerController : Controller
    {
        private readonly Dictionary<Guid, FlowExecutorInfo> Executors = new ();
        private static Mutex mutex = new();

        [HttpPost("work/start")]
        public FlowExecutorInfo StartWork([FromBody] FlowExecutorInfo info)
        {
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
        public void FinishWork([FromBody] FlowExecutorInfo info)
        {
            mutex.WaitOne();
            try
            {
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
                RelativeFile = x.RelativeFile,
                StartedAt = x.StartedAt,
                TotalParts = x.TotalParts,
                Uid = x.Uid,
                WorkingFile = x.WorkingFile
            });
        }

        //[HttpGet("{uid}")]
        //public Worker Get(Guid uid)
        //{
        //    if(Globals.Demo)
        //        return new Worker {    }
        //    FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
        //}

        [HttpGet("{uid}/log")]
        public string Log([FromRoute] Guid uid, [FromQuery] int lineCount = 0)
        {
            FlowExecutorInfo exec = null;
            if (Executors.TryGetValue(uid, out exec) == false)
                return String.Empty;

            if (lineCount == 0)
                return exec.Log;

            var log = exec.Log.Split(new String[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lineCount > 0 && log.Length < lineCount)
                return String.Join(Environment.NewLine, log.Skip(log.Length  - lineCount));
            return String.Join(Environment.NewLine, log);
        }

        [HttpDelete("{uid}")]
        public async Task Abort(Guid uid)
        {
            // need a way to broadcast a cancel command to workers... hmmm grpc... websocket....

            //var worker = FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
            //if (worker == null)
            //    return;

            //await worker.Abort();
        }
    }
}