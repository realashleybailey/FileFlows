namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;

    /// <summary>
    /// Status controller
    /// </summary>
    [Route("/api/status")]
    public class StatusController : Controller
    {
        /// <summary>
        /// Gets if an update is available
        /// </summary>
        /// <returns>True if there is an update</returns>
        [HttpGet("update-available")]
        public object UpdateAvailable()
        {
            var result = Workers.ServerUpdater.GetLatestOnlineVersion();
            return new { UpdateAvailable = result.updateAvailable };
        }

        /// <summary>
        /// Get the current status
        /// </summary>
        /// <returns>the current status</returns>
        [HttpGet]
        public async Task<StatusModel> Get()
        {
            var status = new StatusModel();
            var lfController = new LibraryFileController();
            status.queue = (await lfController.GetAll(FileStatus.Unprocessed))?.Count() ?? 0;
            status.processed = (await lfController.GetAll(FileStatus.Processed))?.Count() ?? 0;
            var workerController = new WorkerController(null);
            var executors = workerController.GetAll()?.ToList() ?? new List<FlowExecutorInfo>();
            status.processing = executors.Count;
            if (executors.Any())
            {
                var time = executors.OrderByDescending(x => x.ProcessingTime).First().ProcessingTime;
                if (time.Hours > 0)
                    status.time = time.ToString(@"h\:mm\:ss");
                else
                    status.time = time.ToString(@"m\:ss");
            }
            else
            {
                status.time = string.Empty;
            }

            return status;
        }

        /// <summary>
        /// The current status
        /// </summary>
        public class StatusModel 
        {
            /// <summary>
            /// Gets the number of items in the queue
            /// </summary>
            public int queue { get; set; }
            /// <summary>
            /// Gets the number of files being processed
            /// </summary>
            public int processing { get; set; }
            /// <summary>
            /// Gets the number of files that have been processed
            /// </summary>
            public int processed { get; set; }
            /// <summary>
            /// Gets the processing time of the longest running item in the queue
            /// </summary>
            public string time { get; set; }
        }
    }
}
