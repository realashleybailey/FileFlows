using System.Text.RegularExpressions;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers
{
    public class FlowWorker : Worker
    {

        public static readonly List<FlowWorker> RegisteredFlowWorkers = new List<FlowWorker>();

        public readonly FlowWorkerStatus Status = new FlowWorkerStatus();

        public FlowLogger CurrentFlowLogger { get; private set; }

        public readonly Guid Uid = Guid.NewGuid();
        private FlowExecutor Executor;

        public FlowWorker() : base(ScheduleType.Second, 5)
        {
            this.Status.Uid = this.Uid;
            RegisteredFlowWorkers.Add(this);
        }


        private static Mutex mutex = new Mutex();
        protected static LibraryFile GetLibraryFile()
        {
            mutex.WaitOne();
            try
            {
                var file = DbHelper.Select<LibraryFile>()
                                   .Where(x => x.Status == FileStatus.Unprocessed)
                                   .OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                                   .ThenBy(x => x.DateCreated)
                                   .FirstOrDefault();
                if (file != null)
                {
                    file.Status = FileStatus.Processing;
                    DbHelper.Update(file);
                }
                return file;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        internal async Task Abort()
        {
            if (Executor != null)
            {
                await Executor.Cancel();
            }
        }

        protected override void Execute()
        {
            var settings = DbHelper.Single<Settings>();
            if (settings?.WorkerFlowExecutor != true)
            {
                Logger.Instance.DLog("Flow executor not enabled");
                return;
            }

            var libFile = GetLibraryFile();
            if (libFile == null)
                return; // nothing to process

            string tempPath = settings.TempPath;
            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
            {
                Logger.Instance.ELog("Temp Path not set, cannot process");
                return;
            }


            try
            {
                FileInfo file = new FileInfo(libFile.Name);
                if (file.Exists == false)
                {
                    DbHelper.Delete<LibraryFile>(libFile.Uid);
                    return;
                }
                var flow = DbHelper.Single<Flow>(libFile.Flow.Uid);
                if (flow == null || flow.Uid == Guid.Empty)
                {
                    libFile.Status = FileStatus.FlowNotFound;
                    DbHelper.Update(libFile);
                    return;
                }

                Logger.Instance.ILog("############################# PROCESSING:  " + file.FullName);
                libFile.ProcessingStarted = System.DateTime.Now;
                DbHelper.Update(libFile);
                this.Status.CurrentFile = libFile.Name;
                this.Status.CurrentUid = libFile.Uid;
                this.Status.RelativeFile = libFile.RelativePath;
                this.Status.Library = libFile.Library;
                this.Status.TotalParts = flow.Parts.Count;
                this.Status.CurrentPart = 0;
                this.Status.CurrentPartPercent = 0;
                this.Status.CurrentPartName = string.Empty;
                this.Status.StartedAt = DateTime.Now;
                this.Status.WorkingFile = libFile.Name;
                Executor = new FlowExecutor();
                CurrentFlowLogger = new FlowLogger
                {
                    File = libFile,
                    LogFile = System.IO.Path.Combine(settings.LoggingPath, libFile.Uid.ToString() + ".log")
                };
                Executor.Logger = CurrentFlowLogger;
                Executor.Flow = flow;
                Executor.OnPartPercentageUpdate += OnPartPercentageUpdate;
                Executor.OnStepChange += OnStepChange;
                libFile.OriginalSize = file.Length;
                Task<Plugin.NodeParameters> task = null;
                try
                {
                    task = Executor.Run(file.FullName, libFile.RelativePath, tempPath, settings.GetLogFile(libFile.Uid));
                    task.Wait();
                }
                finally
                {
                    Executor.OnPartPercentageUpdate -= OnPartPercentageUpdate;
                    Executor.OnStepChange -= OnStepChange;
                    Executor = null;
                }
                Logger.Instance.DLog("FlowWorker.Executor.Status: " + task.Result.Result);
                libFile.Status = task.Result.Result == Plugin.NodeResult.Success ? FileStatus.Processed : FileStatus.ProcessingFailed;
                libFile.OutputPath = task.Result.WorkingFile;
                libFile.FinalSize = new FileInfo(libFile.OutputPath).Length;                
                libFile.ProcessingEnded = System.DateTime.Now;
                DbHelper.Update(libFile);
            }
            finally
            {
                this.Status.CurrentFile = string.Empty;
                this.Status.CurrentUid = Guid.Empty;
                this.Status.RelativeFile = string.Empty;
                this.Status.Library = new ObjectReference { Name = string.Empty, Uid = Guid.Empty };
                this.Status.TotalParts = 0;
                this.Status.CurrentPart = 0;
                this.Status.CurrentPartPercent = 0;
                this.Status.CurrentPartName = string.Empty;
                this.Status.WorkingFile = string.Empty;
                this.Status.StartedAt = new DateTime(1970, 1, 1);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1_000);
                    Trigger();
                });
            }
        }


        private void OnPartPercentageUpdate(float percentage)
        {
            this.Status.CurrentPartPercent = percentage;
        }

        private void OnStepChange(int currentStep, string stepName, string workingFile)
        {
            this.Status.CurrentPart = currentStep;
            this.Status.CurrentPartName = stepName;
            this.Status.WorkingFile = workingFile;
        }
    }
}