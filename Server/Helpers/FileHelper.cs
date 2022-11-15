namespace FileFlows.Server.Helpers;

public class FileHelper
{
    public static bool CreateDirectoryIfNotExists(string directory)
    {
        return Plugin.Helpers.FileHelper.CreateDirectoryIfNotExists(Logger.Instance, directory);
    }

    public static void MoveFile(string source, string destination)
        => File.Move(source, destination);
    
    /// <summary>
    /// Gets the file size of a directory and all its files
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <returns>The directories total size</returns>
    public static long GetDirectorySize(string path)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }    
}
