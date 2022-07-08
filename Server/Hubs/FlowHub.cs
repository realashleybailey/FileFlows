using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using Microsoft.AspNetCore.SignalR;

namespace FileFlows.Server.Hubs;

/// <summary>
/// Signalr Hub for executing flows
/// </summary>
public class FlowHub : Hub
{
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <param name="message">the message to log</param>
    public async Task LogMessage(Guid runnerUid, Guid libraryFileUid, string message)
    {
        try
        {
            await LibraryFileLogHelper.AppendToLog(libraryFileUid, message);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Receives a hello from the flow runner, indicating its still alive and executing
    /// </summary>
    /// <param name="runnerUid">the UID of the flow runner</param>
    /// <param name="libraryFileUid">the UID of the library file</param>
    /// <returns>if the hello was successful or not</returns>
    public Task<bool> Hello(Guid runnerUid, Guid libraryFileUid)
    {
        var result = new WorkerController(null).Hello(runnerUid, libraryFileUid);
        return Task.FromResult(result);
    }
}