namespace FileFlows.Server.Hubs
{
    using FileFlows.Server.Controllers;
    using Microsoft.AspNetCore.SignalR;

    public class FlowHub: Hub
    {

        public async Task LogMessage(Guid libraryFileUid, string message)
        {
            var settings = await new SettingsController().Get();
            Console.Write(libraryFileUid + " => " + message);
            await File.AppendAllTextAsync(Path.Combine(settings.LoggingPath, libraryFileUid + ".log"), message + Environment.NewLine);

        }
    }
}
