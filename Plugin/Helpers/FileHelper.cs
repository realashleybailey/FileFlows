using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileFlows.Plugin.Helpers
{
    public class FileHelper
    {
        public static bool CreateDirectoryIfNotExists(ILogger logger, string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return false;
            var di = new DirectoryInfo(directory);
            if (di.Exists)
                return true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                di.Create();
            else
                CreateLinuxDir(logger, di);
            //if (Chmod(directory))
            //    Logger?.ILog("Succesfully set permissions on directory");
            //else
            //    Logger?.ILog("Failed to set permissions on directory");
            return di.Exists;
        }

        public static bool CreateLinuxDir(ILogger logger, DirectoryInfo di)
        {
            if (di.Exists)
                return true;
            if (di.Parent != null && di.Parent.Exists == false)
            {
                if (CreateLinuxDir(logger, di.Parent) == false)
                    return false;
            }
            logger?.ILog("Creating folder: " + di.FullName);

            string cmd = $"mkdir {EscapePathForLinux(di.FullName)}";

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardError.ReadToEnd();
                    Console.WriteLine(output);
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return ChangeOwner(logger, di.FullName);
                    }
                    logger?.ELog("Failed creating directory:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                    if (string.IsNullOrWhiteSpace(error) == false)
                        logger?.ELog("Error output:" + output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.ELog("Failed creating directory: " + di.FullName + " -> " + ex.Message);
                return false;
            }
        }



        public static bool ChangeOwner(ILogger logger, string filePath, bool recursive = true)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true; // its windows, lets just pretend we did this

            if (filePath.EndsWith(Path.DirectorySeparatorChar) == false)
                filePath += Path.DirectorySeparatorChar;

            logger?.ILog("Changing owner on folder: " + filePath);


            string cmd = $"chown{(recursive ? " -R" : "")} nobody:users {EscapePathForLinux(filePath)}";

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardError.ReadToEnd();
                    Console.WriteLine(output);
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return SetPermissions(logger, filePath);
                    logger?.ELog("Failed changing owner:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                    if (string.IsNullOrWhiteSpace(error) == false)
                        logger?.ELog("Error output:" + output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.ELog("Failed changing owner: " + filePath + " => " + ex.Message);
                return false;
            }
        }


        public static bool SetPermissions(ILogger logger, string filePath, bool recursive = true, bool file = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true; // its windows, lets just pretend we did this

            if (file == false)
            {
                if (filePath.EndsWith(Path.DirectorySeparatorChar) == false)
                    filePath += Path.DirectorySeparatorChar;
            }
            else
            {
                recursive = false;
            }

            logger?.ILog("Setting permissions on folder: " + filePath);


            string cmd = $"chmod{(recursive ? " -R" : "")} 777 {EscapePathForLinux(filePath)}";

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardError.ReadToEnd();
                    Console.WriteLine(output);
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return true;
                    logger?.ELog("Failed setting permissions:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                    if (string.IsNullOrWhiteSpace(error) == false)
                        logger?.ELog("Error output:" + output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.ELog("Failed setting permissions: " + filePath + " => " + ex.Message);
                return false;
            }
        }

        private static string EscapePathForLinux(string path)
        {
            path = Regex.Replace(path, "([\\'\"\\$\\?\\*()\\s])", "\\$1");
            return path;
        }
    }
}
