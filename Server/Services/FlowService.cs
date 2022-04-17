namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class FlowService : IFlowService
    {
        public Task<Flow> Get(Guid uid) => new FlowController().Get(uid);

        public Task<Flow> GetFailureFlow(Guid libraryUid) => new FlowController().GetFailureFlow(libraryUid);

    }
}
