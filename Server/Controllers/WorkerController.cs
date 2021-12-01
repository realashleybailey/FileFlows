namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Workers;

    [Route("/api/worker")]
    public class WorkerController : Controller
    {
        [HttpGet]
        public IEnumerable<FlowWorkerStatus> GetAll()
        {
            return FlowWorker.RegisteredFlowWorkers.Select(x => x.Status);
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
            var worker = FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
            if (worker == null || worker.CurrentFlowLogger == null)
                return string.Empty;
            return worker.CurrentFlowLogger.GetPreview(lineCount);
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