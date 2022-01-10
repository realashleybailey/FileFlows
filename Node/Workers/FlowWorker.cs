namespace FileFlows.Node.Workers
{
    using FileFlows.ServerShared.Services;
    using FileFlows.ServerShared.Workers;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public class FlowWorker : Worker
    {
        public readonly Guid Uid = Guid.NewGuid();

        private readonly List<Guid> ExecutingRunners = new List<Guid>();

        private readonly bool isServer;

        private bool FirstExecute = true;

        public FlowWorker(bool isServer = false) : base(ScheduleType.Second, 10)
        {
            this.isServer = isServer;
            this.FirstExecute = true;
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
                node = isServer ? nodeService.GetServerNode().Result : nodeService.GetByAddress(Environment.MachineName).Result;
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

            if (node?.Enabled != true)
            {
                Logger.Instance?.DLog("Flow executor not enabled");
                return;
            }

            if (node.FlowRunners <= ExecutingRunners.Count)
            {
                Logger.Instance?.DLog("At limit of running executors: " + node.FlowRunners);
                return; // already maximum executors running
            }


            string tempPath = node.TempPath;
            if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
            {
                Logger.Instance?.ELog("Temp Path not set, cannot process");
                return;
            }
            var libFileService = LibraryFileService.Load();
            var libFile = libFileService.GetNext(node.Name, node.Uid, Uid).Result;
            if (libFile == null)
                return; // nothing to process

            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Guid processUid = Guid.NewGuid();
            lock (ExecutingRunners)
            {
                ExecutingRunners.Add(processUid);
            }
            Task.Run(() =>
            {
                try
                {

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
                        isServer ? "--server" : "--notserver"
                    };

#if (DEBUG && false)
                    FileFlows.FlowRunner.Program.Main(parameters);
#else
                    using (Process process = new Process())
                    {
                        try
                        {
                            process.StartInfo = new ProcessStartInfo();
                            if (windows)
                            {
#if (DEBUG)
                                process.StartInfo.FileName = @"C:\Users\john\src\FileFlows\FileFlows\FlowRunner\bin\debug\net6.0\FileFlows.FlowRunner.exe";
#else
                                process.StartInfo.FileName = "FileFlows.FlowRunner.exe";
#endif
                            }
                            else
                            {
                                process.StartInfo.FileName = GetDotnetLocation();
                                process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner");
                            }
#if (!DEBUG)
                            process.StartInfo.WorkingDirectory = Path.Combine(new FileInfo(typeof(FlowWorker).Assembly.Location).DirectoryName, "FileFlows-Runner");
#endif

                            foreach (var str in parameters)
                                process.StartInfo.ArgumentList.Add(str);

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
                            if(process.ExitCode != 0)
                                throw new Exception("Invalid exit code: " + process.ExitCode);

                            SaveLog(libFile, completeLog.ToString());
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
                    lock (ExecutingRunners)
                    {
                        if (ExecutingRunners.Contains(processUid))
                            ExecutingRunners.Remove(processUid);
                    }
                    Trigger();
                }
            });
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
