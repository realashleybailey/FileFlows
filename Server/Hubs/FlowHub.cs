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
}