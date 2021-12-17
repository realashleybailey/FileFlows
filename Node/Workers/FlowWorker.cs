namespace FileFlows.Node.Workers
{
    using FileFlows.Node.FlowExecution;
    using FileFlows.ServerShared.Services;
    using FileFlows.ServerShared.Workers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;

    public class FlowWorker : Worker
    {
        public readonly Guid Uid = Guid.NewGuid();

        private readonly List<FlowRunner> ExecutingRunners = new List<FlowRunner>();

        private readonly bool isServer;

        private bool FirstExecute = true;

        public FlowWorker(bool isServer = false) : base(ScheduleType.Second, 5)
        {
            this.isServer = isServer;
            this.FirstExecute = true;
        }

        public Func<bool> IsEnabledCheck { get; set; }

        protected override void Execute()
        {
            if (IsEnabledCheck?.Invoke() == false)
                return;
            Logger.Instance?.DLog("FlowWorker.Execute");
            var nodeService = NodeService.Load();
            ProcessingNode node;
            try
            {
                node = isServer ? nodeService.GetServerNode().Result : nodeService.GetByAddress(Environment.MachineName).Result;
            }
            catch(Exception ex)
            {
                Logger.Instance?.ELog("Failed to register node: " + ex.Message);
                return;
            }

            if(isServer == false)
                FlowRunnerCommunicator.SignalrUrl = node.SignalrUrl;

            if (FirstExecute)
            {
                FirstExecute = false;
                // tell the server to kill any flow executors from this node, incase this node was restarted
                nodeService.ClearWorkers(node.Uid);
            }

            if (node?.Enabled != true)
            {
                Logger.Instance?.DLog("Flow executor not enabled");
                return;
            }

            if (node.FlowRunners <= ExecutingRunners.Count)
            {
                Logger.Instance?.DLog("At limit of running executors: " + node.FlowRunners);
                return; // already maximum executors running
            }

            string tempPath = node.TempPath;
            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
            {
                Logger.Instance?.ELog("Temp Path not set, cannot process");
                return;
            }

            var libFileService = LibraryFileService.Load();
            var libFile = libFileService.GetNext(node.Uid, Uid).Result;
            if (libFile == null)
                return; // nothing to process

            string workingFile = node.Map(libFile.Name);

            try
            {
                var libfileService = LibraryFileService.Load();
                var flowService = FlowService.Load();
                FileInfo file = new FileInfo(workingFile);
                if (file.Exists == false)
                {
                    libfileService.Delete(libFile.Uid).Wait();
                    return;
                }
                var libService = LibraryService.Load();
                var lib = libService.Get(libFile.Library.Uid).Result;
                if (lib == null)
                {
                    libfileService.Delete(libFile.Uid).Wait();
                    return;
                }

                var flow = flowService.Get(lib.Flow?.Uid ?? Guid.Empty).Result;
                if (flow == null || flow.Uid == Guid.Empty)
                {
                    libFile.Status = FileStatus.FlowNotFound;
                    libfileService.Update(libFile).Wait();
                    return;
                }

                // update the library file to reference the updated flow (if changed)
                if (libFile.Flow.Name != flow.Name || libFile.Flow.Uid != flow.Uid)
                {
                    libFile.Flow = new ObjectReference
                    {
                        Uid = flow.Uid,
                        Name = flow.Name,
                        Type = typeof(Flow)?.FullName ?? String.Empty
                    };
                    libfileService.Update(libFile).Wait();
                }

                Logger.Instance?.ILog("############################# PROCESSING:  " + file.FullName);
                libFile.ProcessingStarted = DateTime.UtcNow;
                libfileService.Update(libFile).Wait();

                var info = new FlowExecutorInfo
                {
                    LibraryFile = libFile,
                    Log = String.Empty,
                    NodeUid = node.Uid,
                    NodeName = node.Name,
                    RelativeFile = libFile.RelativePath,
                    Library = libFile.Library,
                    TotalParts = flow.Parts.Count,
                    CurrentPart = 0,
                    CurrentPartPercent = 0,
                    CurrentPartName = string.Empty,
                    StartedAt = DateTime.UtcNow,
                    WorkingFile = workingFile
                };

                var runner = new FlowRunner(info, flow, node);
                lock (ExecutingRunners)
                {
                    ExecutingRunners.Add(runner);
                }
                runner.OnFlowCompleted += Runner_OnFlowCompleted;
                Task.Run(() => runner.Run());
            }
            finally
            {
                //_ = Task.Run(async () =>
                //{
                //    await Task.Delay(1_000);
                //    Trigger();
                //});
            }
        }

        private void Runner_OnFlowCompleted(FlowRunner sender, bool success)
        {
            System.Threading.Thread.Sleep(5000);
            lock (this.ExecutingRunners)
            {
                if(ExecutingRunners.Contains(sender))
                    ExecutingRunners.Remove(sender);
            }
        }
    }
}
