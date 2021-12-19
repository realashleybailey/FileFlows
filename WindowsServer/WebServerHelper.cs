using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.WindowsServer
{
    internal class WebServerHelper
    {
        static Process process;

        public static void Start()
        {
            WebServerHelper.process = new Process();
            // If you run bash-script on Linux it is possible that ExitCode can be 255.
            // To fix it you can try to add '#!/bin/bash' header to the script.

#if (DEBUG)
            process.StartInfo.FileName = @"C:\Users\john\src\FileFlows\FileFlows\deploy\FileFlows-Windows\FileFlows.Server.exe";
            process.StartInfo.WorkingDirectory = @"C:\Users\john\src\FileFlows\FileFlows\deploy\FileFlows-Windows";
#else
            process.StartInfo.FileName = "FileFlows.Server.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
#endif
            process.StartInfo.Arguments = "--windows --urls=http://[::]:5151";

            process.Start();
            ChildProcessTracker.AddProcess(process);
        }

        public static void Stop()
        {
            try
            {
                if (process != null)
                {
                    Console.Write("Stopping WebServer");
                    process.Kill();
                }
                else
                {

                    Console.Write("WebServer not running");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed stopping webserver: " + ex.Message);
            }
        }

    }
}
