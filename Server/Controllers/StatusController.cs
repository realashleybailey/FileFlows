namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;
    using FileFlows.Plugin;
    using Microsoft.AspNetCore.SignalR;
    using FileFlows.Server.Hubs;

    /// <summary>
    /// Status controller
    /// </summary>
    [Route("/api/status")]
    public class StatusController : Controller
    {
        /// <summary>
        /// Get the current status
        /// </summary>
        /// <returns>the current status</returns>
        [HttpGet]
        public async Task<StatusModel> Get()
        {
            var status = new StatusModel();
            status.Queue = (await new LibraryFileController().GetAll(FileStatus.Unprocessed))?.Count() ?? 0;
            var workerController = new WorkerController(null);
            var executors = workerController.GetAll()?.ToList() ?? new List<FlowExecutorInfo>();
            status.Processing = executors.Count;
            if(executors.Any())
            {
                var time = executors.OrderByDescending(x => x.ProcessingTime).First().ProcessingTime;
                if (time.Hours > 0)
                    status.Time = time.ToString(@"h\:mm\:ss");
                else
                    status.Time = time.ToString(@"m\:ss");
            }
            else
            {
                status.Time = string.Empty;
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
            public int Queue { get; set; }
            /// <summary>
            /// Gets the number of files being processed
            /// </summary>
            public int Processing { get; set; }
            /// <summary>
            /// Gets the processing time of the longest running item in the queue
            /// </summary>
            public string Time { get; set; }
        }
    }
}
