namespace FileFlows.Node.Workers
{
    using FileFlows.ServerShared.Services;
    using FileFlows.ServerShared.Workers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public class FlowWorker : Worker
    {
        public readonly Guid Uid = Guid.NewGuid();

        private Mutex mutex = new Mutex();
        private readonly List<Guid> ExecutingRunners = new ();

        private readonly bool isServer;

        private bool FirstExecute = true;

        private string Hostname { get; set; }

        public FlowWorker(string hostname, bool isServer = false) : base(ScheduleType.Second, 10)
        {
            this.isServer = isServer;
            this.FirstExecute = true;
            this.Hostname = hostname;
        }

        public Func<bool> IsEnabledCheck { get; set; }


        protected override void Execute()
        {
            if (IsEnabledCheck?.Invoke() == false)
                return;
            var nodeService = NodeService.Load();
            ProcessingNode node;
            try
            {
                node = isServer ? nodeService.GetServerNode().Result : nodeService.GetByAddress(this.Hostname).Result;
            }
            catch(Exception ex)
            {
                Logger.Instance?.ELog("Failed to register node: " + ex.Message);
                return;
            }

            if (FirstExecute)
            {
                FirstExecute = false;
                // tell the server to kill any flow executors from this node, incase this node was restarted
                nodeService.ClearWorkers(node.Uid);
            }

            string nodeName = node?.Name == "FileFlowsServer" ? "Internal Processing Node" : (node?.Name ?? "Unknown");

            if (node?.Enabled != true)
            {
                Logger.Instance?.DLog($"Flow executor '{nodeName}' not enabled");
                return;
            }

            if (node.FlowRunners <= ExecutingRunners.Count)
            {
                Logger.Instance?.DLog($"At limit of running executors on '{nodeName}': " + node.FlowRunners);
                return; // already maximum executors running
            }


            string tempPath = node.TempPath;
            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
            {
                Logger.Instance?.ELog($"Temp Path not set on node '{nodeName}, cannot process");
                return;
            }
            var libFileService = LibraryFileService.Load();
            var libFile = libFileService.GetNext(node.Name, node.Uid, Uid).Result;
            if (libFile == null)
                return; // nothing to process

            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Guid processUid = Guid.NewGuid();
            AddExecutingRunner(processUid);
            Task.Run(() =>
            {
                try
                {
#pragma warning disable CS8601 // Possible null reference assignment.
                    var parameters = new string[]
                    {
                        "--uid",
                        processUid.ToString(),
                        "--libfile",
                        libFile.Uid.ToString(),
                        "--tempPath",
                        tempPath,
                        "--baseUrl",
                        Service.ServiceBaseUrl,
                        isServer ? null : "--hostname",
                        isServer ? null : Hostname,
                        isServer ? "--server" : "--notserver"
                    }.Where(x => x != null).ToArray();
#pragma warning restore CS8601 // Possible null reference assignment.

#if (DEBUG && false)
                    FileFlows.FlowRunner.Program.Main(parameters);
#else
                    using (Process process = new Process())
                    {
                        try
                        {
                            process.StartInfo = new ProcessStartInfo();
                            string dir = Path.Combine(new FileInfo(typeof(FlowWorker).Assembly.Location).DirectoryName, "FileFlows-Runner");
                            if (windows)
                            {
#if (DEBUG)
                                process.StartInfo.FileName = @"D:\src\FileFlows\FileFlows\FlowRunner\bin\debug\net6.0\FileFlows.FlowRunner.exe";
#else
                                string flowRunner = Path.Combine(dir, "FileFlows.FlowRunner" + (windows ? ".exe" : ".dll"));
                                process.StartInfo.FileName = flowRunner;
#endif
                            }
                            else
                            {
                                process.StartInfo.FileName = GetDotnetLocation();
                                process.StartInfo.WorkingDirectory = dir;
                                process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
                            }
                            foreach (var str in parameters)
                                process.StartInfo.ArgumentList.Add(str);

                            Logger.Instance?.ILog("Executing: " + process.StartInfo.FileName + " " + String.Join(" ", process.StartInfo.ArgumentList.Select(x => "\"" + x + "\"")));
                            Logger.Instance?.ILog("Working Directory: " + process.StartInfo.WorkingDirectory);

                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            process.StartInfo.CreateNoWindow = true;
                            process.Start();
                            string output = process.StandardOutput.ReadToEnd();
                            StringBuilder completeLog = new StringBuilder();
                            if (string.IsNullOrEmpty(output) == false)
                            {
                                completeLog.AppendLine(
                                    "==============================================================================" + Environment.NewLine +
                                    "===                      PROCESSING NODE OUTPUT START                      ===" + Environment.NewLine +
                                    "==============================================================================" + Environment.NewLine +
                                    output + Environment.NewLine +
                                    "==============================================================================" + Environment.NewLine +
                                    "===                       PROCESSING NODE OUTPUT END                       ===" + Environment.NewLine +
                                    "==============================================================================");
                            }
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            if (string.IsNullOrEmpty(error) == false)
                            {
                                completeLog.AppendLine(
                                    "==============================================================================" + Environment.NewLine +
                                    "===                   PROCESSING NODE ERROR OUTPUT START                   ===" + Environment.NewLine +
                                    "==============================================================================" + Environment.NewLine +
                                    error  + Environment.NewLine +
                                    "==============================================================================" + Environment.NewLine +
                                    "===                    PROCESSING NODE ERROR OUTPUT END                    ===" + Environment.NewLine +
                                    "==============================================================================");
                            }

                            SaveLog(libFile, completeLog.ToString());

                            if (process.ExitCode != 0)
                                throw new Exception("Invalid exit code: " + process.ExitCode);

                        }
                        catch (Exception ex)
                        {
                            Logger.Instance?.ELog("Error executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                            libFile.Status = FileStatus.ProcessingFailed;
                            libFileService.Update(libFile);
                        }
                    }
                    #endif
                }
                finally
                {
                    RemoveExecutingRunner(processUid);

                    try
                    {
                        string dir = Path.Combine(tempPath, "Runner-" + processUid.ToString());
                        if(Directory.Exists(dir))
                            Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance?.WLog("Failed to clean up runner directory: " + ex.Message);
                    }

                    Trigger();
                }
            });
        }


        private void AddExecutingRunner(Guid uid)
        {
            mutex.WaitOne();
            try
            {
                ExecutingRunners.Add(uid);
            }
            finally
            {
                mutex.ReleaseMutex();   
            }
        }

        private void RemoveExecutingRunner(Guid uid)
        {
            Logger.Instance?.ILog($"Removing executing runner[{ExecutingRunners.Count}]: {uid}");
            mutex.WaitOne();
            try
            {
                if (ExecutingRunners.Contains(uid))
                    ExecutingRunners.Remove(uid);
                else
                {
                    Logger.Instance?.ILog("Exeucting runner not in list: " + uid +" => " + String.Join(",", ExecutingRunners.Select(x => x.ToString())));
                }
                Logger.Instance?.ILog("Runner count: " + ExecutingRunners.Count);
            }
            catch(Exception ex)
            {
                Logger.Instance?.ELog("Failed to remove executing runner: " + ex.Message + Environment.NewLine + ex.StackTrace);    
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private void SaveLog(LibraryFile libFile, string log)
        { 
            var service = new LibraryFileService();
            bool saved = service.SaveFullLog(libFile.Uid, log).Result;
            if (!saved)
            {
                // save to main output
                log = string.Join('\n', log.Split('\n').Select(x => "       " + x).ToArray());
                Logger.Instance?.DLog(Environment.NewLine + log);
            }
        }


        private static string Dotnet = "";
        private string GetDotnetLocation()
        {
            if(string.IsNullOrEmpty(Dotnet))
            {
                if (File.Exists("/root/.dotnet/dotnet"))
                    Dotnet = "/root/.dotnet/dotnet"; // location of docker
                else
                {
                    Dotnet = "dotnet";// assume in PATH
                }
            }
            return Dotnet;
        }
    }
}
