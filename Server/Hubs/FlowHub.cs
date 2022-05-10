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
                string filename = (await new SettingsController().Get()).GetLogFile(DirectoryHelper.LoggingDirectory, libraryFileUid);
                var fi = new FileInfo(filename);
                if(fi.Directory.Exists == false)
                    fi.Directory.Create();
                await File.AppendAllTextAsync(fi.FullName, message + Environment.NewLine);
            }
            catch (Exception) { }

        }
    }
}
