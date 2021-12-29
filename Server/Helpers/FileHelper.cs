namespace FileFlows.Server.Helpers
{
    public class FileHelper
    {
        public static bool CreateDirectoryIfNotExists(string directory)
        {
            return Plugin.Helpers.FileHelper.CreateDirectoryIfNotExists(Logger.Instance, directory); ;
        }

        public static void MoveFile(string source, string destination)
        {
            System.IO.File.Move(source, destination);
        }
    }
}
