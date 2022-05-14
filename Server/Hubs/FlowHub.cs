using FileFlows.Server.Helpers;

namespace FileFlows.Server.Hubs
{
    using FileFlows.Server.Controllers;
    using Microsoft.AspNetCore.SignalR;

    public class FlowHub: Hub
    {

        public async Task LogMessage(Guid runnerUid, Guid libraryFileUid, string message)
        {
            try
            {
                await LibraryFileLogHelper.AppendToLog(libraryFileUid, message);
            }
            catch (Exception) { }

        }
    }
}
