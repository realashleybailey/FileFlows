namespace FileFlow.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Shared.Models;
    using FileFlow.Server.Workers;

    [Route("/api/worker")]
    public class WorkerController : Controller
    {
        [HttpGet]
        public IEnumerable<FlowWorkerStatus> GetAll() => FlowWorker.RegisteredFlowWorkers.Select(x => x.Status);

        [HttpGet("{uid}")]
        public Worker Get(Guid uid) => FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);

        [HttpGet("{uid}/log")]
        public string Log(Guid uid)
        {
            var worker = FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
            if (worker == null || worker.CurrentFlowLogger == null)
                return string.Empty;
            return worker.CurrentFlowLogger.GetPreview();
        }

        [HttpDelete("{uid}")]
        public async Task Abort(Guid uid)
        {
            var worker = FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
            if (worker == null)
                return;

            await worker.Abort();
        }
    }
}