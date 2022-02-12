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
        static bool Stopping = false;
        static DateTime LastStarted = DateTime.MinValue;
        static System.Timers.Timer timer;

        public static void Start()
        {
            if (timer == null)
            {
                timer = new System.Timers.Timer();
                timer.Interval = 10_000;
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = true;
            }
            timer.Start();
            try
            {
                WebServerHelper.process = new Process();
                // If you run bash-script on Linux it is possible that ExitCode can be 255.
                // To fix it you can try to add '#!/bin/bash' header to the script.

#if (DEBUG)
                process.StartInfo.FileName = @"D:\src\FileFlows\FileFlows\Server\bin\Release\net6.0\win-x64\FileFlows.Server.exe";
                process.StartInfo.WorkingDirectory = @"D:\src\FileFlows\FileFlows\Server\bin\Release\net6.0\win-x64";
#else
            process.StartInfo.FileName = "FileFlows.Server.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = Logger.GetAppDirectory();
#endif
                process.StartInfo.Arguments = "--windows --urls=http://[::]:5151";
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;

                LastStarted = DateTime.Now;
                Logger.ILog("Starting FileFlows.Server: " + process.StartInfo.Arguments);
                process.Start();
                ChildProcessTracker.AddProcess(process);
            }
            catch (Exception ex)
            {
                Logger.ELog("Failed starting FileFlowsServer: " + ex.Message + Environment.NewLine + ex.StackTrace);
                Application.Exit();
            }
        }

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (Stopping)
                return;
            // check if process is running
           // if(process != null && process.HasExited == false && process.
            {
                // 
            }
        }

        private static void Process_Exited(object? sender, EventArgs e)
        {
            int exitCode = process.ExitCode;
            Logger.ILog("FileFlows.Server processed exited: " + exitCode);
            if(exitCode == 99)
            {
                // special code for upgrade
                Form1.Instance?.QuitMe();
            }
            else if(Stopping == false)
            {
                // process exited unexpectably, restart it
                // but only if it last started more than 20 seconds ago, otherwise we could be in a crash loop
                if (LastStarted < DateTime.Now.AddSeconds(-20))
                {
                    Start();
                }
                else
                {
                    // close the main server, something went wrong
                    Form1.Instance?.QuitMe();
                }
            }
        }

        public static void Stop()
        {
            Stopping = true;
            try
            {
                if(timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }
                if (process != null)
                {
                    Logger.ILog("Stopping WebServer");
                    process.Kill();
                }
                else
                {

                    Logger.ILog("WebServer not running");
                }


                var processes = Process.GetProcessesByName("FileFlows.Server");
                foreach (var p in processes)
                    p.Kill();
            }
            catch (Exception ex)
            {
                Logger.WLog("Failed stopping webserver: " + ex.Message);
            }
            Stopping = false;
        }

    }
}
