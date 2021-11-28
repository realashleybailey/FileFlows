using FileFlows.Server.Helpers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers
{
    public class TelemetryReporter: Worker
    {
        public TelemetryReporter() : base(ScheduleType.Daily, 0)
        {
            Trigger();
        }

        protected override void Execute()
        {
            var settings = DbHelper.Single<Settings>();
            if (settings?.DisableTelemetry == true)
                return; // they have turned it off, dont report anything

            TelemetryData data = new TelemetryData();
            data.ClientUid = settings.Uid;
            var libFiles = DbHelper.Select<LibraryFile>();
            data.FilesFailed = libFiles.Where(x => x.Status == FileStatus.ProcessingFailed).Count();
            data.FilesProcessed = libFiles.Where(x => x.Status == FileStatus.Processed).Count();
            var flows = DbHelper.Select<Flow>();
            data.Nodes = flows.SelectMany(x => x.Parts)
                              .GroupBy(x => x.Name)
                              .Select(x => new TelemetryNode
                              {
                                  Name = x.Key,
                                  Count = x.Count()
                              }).ToList();

#if(DEBUG)
            var task = HttpHelper.Put("https://localhost:7197/api/telemetry", data);
#else
            var task = HttpHelper.Put("http://fileflows.com/api/telemetry", data);
#endif
            task.Wait();

        }



        public class TelemetryData
        {
            public Guid ClientUid { get; set; }

            public List<TelemetryNode> Nodes { get; set; }

            public int FilesProcessed { get; set; }
            public int FilesFailed { get; set; }
        }

        public class TelemetryNode
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }
    }
}
