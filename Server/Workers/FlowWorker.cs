using System.Text.RegularExpressions;
using FileFlow.Server.Controllers;
using FileFlow.Server.Helpers;
using FileFlow.Shared;
using FileFlow.Shared.Models;

namespace FileFlow.Server.Workers
{
    public class FlowWorker : Worker
    {

        public FlowWorker() : base(ScheduleType.Second, 5) { }


        private static Mutex mutex = new Mutex();
        protected static LibraryFile GetLibraryFile()
        {
            mutex.WaitOne();
            try
            {
                var file = DbHelper.Select<LibraryFile>()
                                   .Where(x => x.Status == FileStatus.Unprocessed)
                                   .OrderBy(x => x.Order != -1 ? x.Order : int.MaxValue)
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


        protected override void Execute()
        {
            var libFile = GetLibraryFile();
            if (libFile == null)
                return; // nothing to process

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
                var executor = new FlowExecutor();
                executor.Logger = new FlowLogger
                {
                    File = libFile
                };
                executor.Flow = flow;
                var task = executor.Run(file.FullName);
                task.Wait();
                libFile.Status = task.Result.Result == Plugin.NodeResult.Success ? FileStatus.Processed : FileStatus.ProcessingFailed;
                DbHelper.Update(libFile);
            }
            finally
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1_000);
                    Trigger();
                });
            }
        }
    }
}