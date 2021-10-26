namespace ViWatcher.Server.Controllers
{
    using System;
    using System.Diagnostics;    
    using Microsoft.AspNetCore.Mvc;
    using ViWatcher.Server;
    using ViWatcher.Shared;
    using ViWatcher.Shared.Models;
    using ViWatcher.Server.Helpers;

    [Route("/api/flow")]
    public class FlowController : Controller
    {

        [HttpGet("elements")]
        public IEnumerable<FlowElement> GetElements()
        {
            var results = DbHelper.Select<FlowElement>().ToArray();
            if(results.Length == 0){
                return new[] {
                    new FlowElement { Uid = Guid.NewGuid(), Inputs = 0, Outputs = 1, Name = "Video File", Type = FlowElementType.Input },
                    new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 2, Name = "Function", Type = FlowElementType.Logic },
                    new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 1, Name = "h265", Type = FlowElementType.Process },
                    new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 1, Name = "AC3", Type = FlowElementType.Process },
                    new FlowElement { Uid = Guid.NewGuid(), Inputs = 1, Outputs = 0, Name = "Output File", Type = FlowElementType.Output },
                };
            }
            return results;
        }

        [HttpGet("one")]
        public Flow Get() => DbHelper.Single<Flow>();

        [HttpPut]
        public Flow Save([FromBody] Flow model)
        {
            if(model == null)
                throw new Exception("No model");
            var flow = DbHelper.Update<Flow>(model);
            return flow;
        }
    }

}