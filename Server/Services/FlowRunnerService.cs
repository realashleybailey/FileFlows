namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class FlowRunnerService : IFlowRunnerService
    {
        public Task<FlowExecutorInfo> Start(FlowExecutorInfo info) => Task.FromResult(new WorkerController().StartWork(info));
        public Task Complete(FlowExecutorInfo info)
        {
            new WorkerController().FinishWork(info);
            return Task.CompletedTask;
        }

        public Task Update(FlowExecutorInfo info)
        {
            new WorkerController().UpdateWork(info);
            return Task.CompletedTask;
        }
    }
}
