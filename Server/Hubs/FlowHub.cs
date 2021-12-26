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
            try
            {
                var fi = new FileInfo(Path.Combine(settings.LoggingPath, libraryFileUid + ".log"));
                if(fi.Directory.Exists == false)
                    fi.Directory.Create();
                await File.AppendAllTextAsync(fi.FullName, message + Environment.NewLine);
            }
            catch (Exception ex) { }

        }
    }
}
