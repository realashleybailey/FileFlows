namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class FlowRunnerService : IFlowRunnerService
    {
        public Task<FlowExecutorInfo> Start(FlowExecutorInfo info) => new WorkerController(null).StartWork(info);
        public Task Complete(FlowExecutorInfo info)
        {
            new WorkerController(null).FinishWork(info);
            return Task.CompletedTask;
        }

        public Task Update(FlowExecutorInfo info)
        {
            new WorkerController(null).UpdateWork(info);
            return Task.CompletedTask;
        }
    }
}
