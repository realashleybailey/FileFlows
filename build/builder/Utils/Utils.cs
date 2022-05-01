using System.Diagnostics;
using System.IO.Compression;

public static class Utils
{
    public static void CopyLauncher(string name, string path){
        File.Copy(BuildOptions.SourcePath + "/build/utils/WindowsLauncher.exe", path + "/" + name + ".exe");
    }

    public static string GetGitBranch()
    {
        using var git = new System.Diagnostics.Process();
        git.StartInfo.UseShellExecute = false;
        git.StartInfo.RedirectStandardError = true;
        git.StartInfo.RedirectStandardOutput = true;
        git.StartInfo.CreateNoWindow = true;
        git.StartInfo.FileName = BuildOptions.IsWindows ? "git.exe" : "git";
        git.StartInfo.Arguments = "rev-parse --abbrev-ref HEAD";
        git.Start();
        string output = git.StandardOutput.ReadToEnd().Trim();
        string error = git.StandardError.ReadToEnd().Trim();
        git.WaitForExit();
        git.Dispose();

        return output;
    }

    public static int GetBuildNumber()
    {
        var git = new System.Diagnostics.Process();
        git.StartInfo.UseShellExecute = false;
        git.StartInfo.RedirectStandardError = true;
        git.StartInfo.RedirectStandardOutput = true;
        git.StartInfo.CreateNoWindow = true;
        git.StartInfo.FileName = BuildOptions.IsWindows ? "git.exe" : "git";
        git.StartInfo.Arguments = "rev-list --count HEAD";
        git.Start();
        string output = git.StandardOutput.ReadToEnd().Trim();
        string error = git.StandardError.ReadToEnd().Trim();
        git.WaitForExit();

        Logger.ILog("Git Build Number Output" +  Environment.NewLine + output + Environment.NewLine + error);

        // The output is the number of revisions till HEAD
        if(int.TryParse(output, out int buildNumber))
            return buildNumber;

        if(File.Exists(BuildOptions.SourcePath + "/gitversion.txt"))
        {
            string txt = File.ReadAllText(BuildOptions.SourcePath + "/gitversion.txt").Trim();            
            Logger.ILog("Version from gitversion.txt: " + txt);
            if(int.TryParse(txt, out int buildNumber2))
                return buildNumber2;
        }
        
        return 0;
    }

    public static void RegexReplace(string file, string pattern, string replacement)
    {
        string contents = File.ReadAllText(file);
        string replaced = Regex.Replace(contents, pattern, replacement);
        if (replaced != contents)
            File.WriteAllText(file, replaced);
    }


    public static void EnsureDirectoryExists(string path, bool clean = false)
    {
        if (Directory.Exists(path) == false)
        {
            Logger.ILog("Creating directory: " + path);
            Directory.CreateDirectory(path);
        }
        else if (clean)
            CleanDirectory(path);
    }

    public static void CleanDirectory(string path)
    {
        if (Directory.Exists(path) == false)
            return;

        Logger.ILog("Cleaning directory: " + path);

        foreach (string file in Directory.GetFiles(path))
        {
            File.Delete(file);
        }

        foreach (string dir in Directory.GetDirectories(path))
        {
            Directory.Delete(dir, true);
        }
        return;
    }

    public static void DeleteDirectoryIfExists(string path)
    {
        try
        {
            if (System.IO.Directory.Exists(path) == false)
                return;
            Logger.ILog("Deleting Directory: " + path);
            System.IO.Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            Logger.ILog($"Failed to delete directory '{path}': {ex.Message}");
        }
    }

    public static (int exitCode, string output, string error) Exec(string cmd, string parameters = "")
    {
        using (var process = new Process())
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = cmd;
            process.StartInfo.Arguments = parameters;

            string line = "Executing " + cmd + " " + parameters;
            Logger.DLog(new string('=', Math.Min(100, line.Length)));
            Logger.DLog(line);
            Logger.DLog(new string('=', Math.Min(100, line.Length)));

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();
            if (string.IsNullOrEmpty(output) == false)
                Logger.ILog(output);
            if (string.IsNullOrEmpty(error) == false)
                Logger.WLog(error);

            return (process.ExitCode, output, error);
        }
    }


    public static (int exitCode, string output, string error) Exec(string cmd, string[] parameters)
    {
        parameters ??= new string[] { };
        using (var process = new Process())
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = cmd;
            foreach(var arg in parameters)
                process.StartInfo.ArgumentList.Add(arg);

            string line = ("Executing " + cmd + " " + String.Join(" ", parameters.Select(x => "\"" + x + "\""))).Trim();
            Logger.DLog(new string('=', Math.Min(100, line.Length)));
            Logger.DLog(line);
            Logger.DLog(new string('=', Math.Min(100, line.Length)));

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();
            if (string.IsNullOrEmpty(output) == false)
                Logger.ILog(output);
            if (string.IsNullOrEmpty(error) == false)
                Logger.WLog(error);

            return (process.ExitCode, output, error);
        }
    }

    /// <summary>
    /// Zips up a path
    /// </summary>
    /// <param name="path">the path to zip up </param>
    /// <param name="filename">The filename of the zip to create</param>
    public static void Zip(string path, string filename)
    {
        path = new DirectoryInfo(path).FullName;
        var fileInfo = new FileInfo(filename);
        if (fileInfo.Directory.Exists == false)
            fileInfo.Directory.Create();
        filename = fileInfo.FullName;
        if (File.Exists(filename))
            File.Delete(filename);

        ZipFile.CreateFromDirectory(path, filename);
        Logger.ILog("Created zip file: " + filename);
    }

    public static void ZipFiles(string zipFile, params string[] filesToZip)
    {
        DeleteFile(zipFile);
        Logger.ILog("Creating zip: " + zipFile);
        using ZipArchive zip = ZipFile.Open(zipFile, ZipArchiveMode.Create);
        foreach (string file in filesToZip)
        {
            var fi = new FileInfo(file);
            zip.CreateEntryFromFile(fi.FullName, fi.Name);
        }
    }

    public static void CopyFile(string sourceFileName, string destFileName)
    {
        var source = new FileInfo(sourceFileName);
        if (source.Exists == false)
            throw new Exception("File does not exist: " + sourceFileName);

        if (Directory.Exists(destFileName))
        {
            // dest is a folder
            string dest = Path.Combine(destFileName, source.Name);
            File.Copy(source.FullName, dest, true);
            Logger.ILog($"Copied file '{source.FullName}' to '{dest}'");
        }
        else
        {

            var fiDestFile = new FileInfo(destFileName);

            if (fiDestFile.Directory.Exists == false)
                fiDestFile.Directory.Create();

            File.Copy(source.FullName, fiDestFile.FullName, true);
            Logger.ILog($"Copied file '{source.FullName}' to '{fiDestFile.FullName}'");
        }
    }

    public static void DeleteFile(string filename)
    {
        var fi = new FileInfo(filename);
        if (fi.Exists == false)
            return;
        fi.Delete();
        Logger.ILog("Deleted file: " + fi.FullName);
    }

    public static void DeleteFiles(string path, string filename, bool recurisve = false)
    {
        var dir = new DirectoryInfo(path);
        if (dir.Exists == false)
            return;

        foreach (var file in dir.EnumerateFiles(filename, recurisve ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            file.Delete();
            Logger.ILog("Deleted file: " + file.FullName);
        }
    }

    public static void CopyFiles(string sourcePath, string targetPath, bool recursive = true, string pattern = "")
    {
        sourcePath = new DirectoryInfo(sourcePath).FullName;
        targetPath = new DirectoryInfo(targetPath).FullName;

        if (Directory.Exists(sourcePath) == false)
            throw new Exception("Source directory not found: " + sourcePath);
        EnsureDirectoryExists(targetPath);

        // Now Create all of the directories
        if (recursive)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }
        }

        // Copy all the files & Replaces any files with the same name
        int count = 0;
        bool hasPattern = string.IsNullOrEmpty(pattern) == false;
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        {
            if (hasPattern)
            {
                if (Regex.IsMatch(newPath, pattern) == false)
                    continue;
            }
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            ++count;
        }
        if (count == 0)
            throw new Exception("No files copied from: " + sourcePath);
        Logger.ILog($"Copied {count} files '{sourcePath}' to '{targetPath}'");
    }
}