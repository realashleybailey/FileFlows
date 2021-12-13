//namespace FileFlows.Server.Workers
//{
//    using System.Text.RegularExpressions;
//    using FileFlows.Server.Controllers;
//    using FileFlows.Server.Helpers;
//    using FileFlows.Shared;
//    using FileFlows.Shared.Models;

//    public class FlowWorker : Worker
//    {
//        public static readonly List<FlowWorker> RegisteredFlowWorkers = new List<FlowWorker>();

//        public readonly FlowWorkerStatus Status = new FlowWorkerStatus();

//        public FlowLogger CurrentFlowLogger { get; private set; }

//        public readonly Guid Uid = Guid.NewGuid();
//        private FlowExecutor Executor;

//        public FlowWorker() : base(ScheduleType.Second, 5)
//        {
//            this.Status.Uid = this.Uid;
//            RegisteredFlowWorkers.Add(this);
//        }

//        internal async Task Abort()
//        {
//            if (Executor != null)
//            {
//                await Executor.Cancel();
//            }
//        }

//        protected override void Execute()
//        {
//            var settings = new SettingsController().Get().Result;
//            if (settings?.WorkerFlowExecutor != true)
//            {
//                //Logger.Instance.DLog("Flow executor not enabled");
//                return;
//            }


//            var libFileService = ServerShared.Services.LibraryFileService.Load();
//            var libFile = libFileService.GetNext(Guid.NewGuid(), 1).Result;
//            if (libFile == null)
//                return; // nothing to process

//            string tempPath = settings.TempPath;
//            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
//            {
//                Logger.Instance.ELog("Temp Path not set, cannot process");
//                return;
//            }


//            try
//            {
//                var controller = new LibraryFileController();
//                var flowController = new FlowController();
//                FileInfo file = new FileInfo(libFile.Name);
//                if (file.Exists == false)
//                {
//                    controller.DeleteAll(libFile.Uid).Wait();
//                    return;
//                }
//                var lib = new LibraryController().Get(libFile.Library.Uid).Result;
//                if(lib == null)
//                {
//                    controller.DeleteAll(libFile.Uid).Wait();
//                    return;
//                }

//                var flow = flowController.Get(lib.Flow?.Uid ?? Guid.Empty).Result;
//                if (flow == null || flow.Uid == Guid.Empty)
//                {
//                    libFile.Status = FileStatus.FlowNotFound;
//                    controller.Update(libFile).Wait();
//                    return;
//                }

//                // update the library file to reference the updated flow (if changed)
//                if (libFile.Flow.Name != flow.Name || libFile.Flow.Uid != flow.Uid)
//                {
//                    libFile.Flow = new ObjectReference
//                    {
//                        Uid = flow.Uid,
//                        Name = flow.Name,
//                        Type = typeof(Flow)?.FullName ?? String.Empty
//                    };
//                    controller.Update(libFile).Wait();
//                }


//                Logger.Instance.ILog("############################# PROCESSING:  " + file.FullName);
//                libFile.ProcessingStarted = DateTime.UtcNow;
//                controller.Update(libFile).Wait();
//                this.Status.CurrentFile = libFile.Name;
//                this.Status.CurrentUid = libFile.Uid;
//                this.Status.RelativeFile = libFile.RelativePath;
//                this.Status.Library = libFile.Library;
//                this.Status.TotalParts = flow.Parts.Count;
//                this.Status.CurrentPart = 0;
//                this.Status.CurrentPartPercent = 0;
//                this.Status.CurrentPartName = string.Empty;
//                this.Status.StartedAt = DateTime.UtcNow;
//                this.Status.WorkingFile = libFile.Name;
//                Executor = new FlowExecutor();
//                CurrentFlowLogger = new FlowLogger
//                {
//                    File = libFile,
//                    LogFile = System.IO.Path.Combine(settings.LoggingPath, libFile.Uid.ToString() + ".log")
//                };
//                Executor.Logger = CurrentFlowLogger;
//                Executor.Flow = flow;
//                Executor.OnPartPercentageUpdate += OnPartPercentageUpdate;
//                Executor.OnStepChange += OnStepChange;
//                libFile.OriginalSize = file.Length;
//                Task<Plugin.NodeParameters> task = null;
//                try
//                {
//                    task = Executor.Run(file.FullName, libFile.RelativePath, tempPath, settings.GetLogFile(libFile.Uid));
//                    task.Wait();
//                }
//                finally
//                {
//                    Executor.OnPartPercentageUpdate -= OnPartPercentageUpdate;
//                    Executor.OnStepChange -= OnStepChange;
//                    Executor = null;
//                }
//                Logger.Instance.DLog("FlowWorker.Executor.Status: " + task.Result.Result);
//                libFile.Status = task.Result.Result == Plugin.NodeResult.Success ? FileStatus.Processed : FileStatus.ProcessingFailed;
//                libFile.OutputPath = task.Result.WorkingFile;
//                libFile.FinalSize = new FileInfo(libFile.OutputPath).Length;
//                libFile.ProcessingEnded = DateTime.UtcNow;
//                controller.Update(libFile).Wait();
//            }
//            finally
//            {
//                this.Status.CurrentFile = string.Empty;
//                this.Status.CurrentUid = Guid.Empty;
//                this.Status.RelativeFile = string.Empty;
//                this.Status.Library = new ObjectReference { Name = string.Empty, Uid = Guid.Empty };
//                this.Status.TotalParts = 0;
//                this.Status.CurrentPart = 0;
//                this.Status.CurrentPartPercent = 0;
//                this.Status.CurrentPartName = string.Empty;
//                this.Status.WorkingFile = string.Empty;
//                this.Status.StartedAt = new DateTime(1970, 1, 1);
//                _ = Task.Run(async () =>
//                {
//                    await Task.Delay(1_000);
//                    Trigger();
//                });
//            }
//        }


//        private void OnPartPercentageUpdate(float percentage)
//        {
//            this.Status.CurrentPartPercent = percentage;
//        }

//        private void OnStepChange(int currentStep, string stepName, string workingFile)
//        {
//            this.Status.CurrentPart = currentStep;
//            this.Status.CurrentPartName = stepName;
//            this.Status.WorkingFile = workingFile;
//        }
//    }
//}