using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileFlows.FlowRunner
{
    public class Program
    {
        public static Guid Uid { get; private set; }

        public static void Main(string[] args)
        {
            int exitCode = 0;
            try
            {
                args ??= new string[] { };
                bool server = args.Any(x => x.ToLower() == "--server");

                string uid = GetArgument(args, "--uid");
                if (string.IsNullOrEmpty(uid))
                    throw new Exception("uid not set.");
                Uid = Guid.Parse(uid);

                string tempPath = GetArgument(args, "--tempPath");
                if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
                    throw new Exception("Temp path doesnt exist: " + tempPath);

                string baseUrl = GetArgument(args, "--baseUrl");
                if (string.IsNullOrEmpty(baseUrl))
                    throw new Exception("baseUrl not set");
                LogInfo("Base URL: " + baseUrl);
                Service.ServiceBaseUrl = baseUrl;


                string hostname = GetArgument(args, "--hostname");
                if(string.IsNullOrWhiteSpace(hostname))
                    hostname = Environment.MachineName;


                string workingDir = Path.Combine(tempPath, "Runner-" + uid);
                Directory.CreateDirectory(workingDir);

                var libfileUid = Guid.Parse(GetArgument(args, "--libfile"));
                Shared.Helpers.HttpHelper.Client = new HttpClient();
                Execute(server, tempPath, libfileUid, workingDir, hostname);
            }
            catch (Exception ex)
            {
                exitCode = 1;
                LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                while(ex.InnerException != null)
                {
                    LogInfo("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    ex = ex.InnerException;
                }
                return;
            }
            finally
            {
                LogInfo("Exit Code: " + exitCode);
                Environment.ExitCode = exitCode;
            }
        }

        static string GetArgument(string[] args, string name)
        {
            int index = args.Select(x => x.ToLower()).ToList().IndexOf(name.ToLower());
            if (index < 0)
                return string.Empty;
            if (index >= args.Length - 1)
                return string.Empty;
            return args[index + 1];
        }


        static void Execute(bool isServer, string tempPath, Guid libfileUid, string workingDir, string hostname)
        {
            ProcessingNode node;
            var nodeService = NodeService.Load();
            try
            {
                string address = isServer ? "INTERNAL_NODE" : hostname;
                LogInfo("Address: "+ address);
                var nodeTask = nodeService.GetByAddress(address);
                LogInfo("Waiting on node task");
                nodeTask.Wait();
                LogInfo("Completed node task");
                node = nodeTask.Result;
                if (node == null)
                    throw new Exception("Failed to load node!!!!");
                LogInfo("Node SignalrUrl: " + node.SignalrUrl);
            }
            catch (Exception ex)
            {
                LogInfo("Failed to register node: " + ex.Message + Environment.NewLine + ex.StackTrace);
                throw;
            }

            FlowRunnerCommunicator.SignalrUrl = node.SignalrUrl;

            var libFileService = LibraryFileService.Load();
            var libFile = libFileService.Get(libfileUid).Result;
            if (libFile == null)
            {
                LogInfo("Library file not found, must have been deleted from the library files.  Nothing to process");
                return; // nothing to process
            }

            string workingFile = node.Map(libFile.Name);

            var libfileService = LibraryFileService.Load();
            var libService = LibraryService.Load();
            var lib = libService.Get(libFile.Library.Uid).Result;
            if (lib == null)
            {
                LogInfo("Library was null, deleting library file");
                libfileService.Delete(libFile.Uid).Wait();
                return;
            }

            var flowService = FlowService.Load();
            FileSystemInfo file = lib.Folders ? new DirectoryInfo(workingFile) : new FileInfo(workingFile);
            if (file.Exists == false)
            {
                LogInfo("Library file does not exist, deleting from library files: " + file.FullName);
                libfileService.Delete(libFile.Uid).Wait();
                return;
            }

            var flow = flowService.Get(lib.Flow?.Uid ?? Guid.Empty).Result;
            if (flow == null || flow.Uid == Guid.Empty)
            {
                LogInfo("Flow not found, cannot process file: " + file.FullName);
                libFile.Status = FileStatus.FlowNotFound;
                libfileService.Update(libFile).Wait();
                return;
            }

            // update the library file to reference the updated flow (if changed)
            if (libFile.Flow?.Name != flow.Name || libFile.Flow?.Uid != flow.Uid)
            {
                libFile.Flow = new ObjectReference
                {
                    Uid = flow.Uid,
                    Name = flow.Name,
                    Type = typeof(Flow)?.FullName ?? String.Empty
                };
                libfileService.Update(libFile).Wait();
            }

            libFile.ProcessingStarted = DateTime.UtcNow;
            libfileService.Update(libFile).Wait();

            var info = new FlowExecutorInfo
            {
                LibraryFile = libFile,
                Log = String.Empty,
                NodeUid = node.Uid,
                NodeName = node.Name,
                RelativeFile = libFile.RelativePath,
                Library = libFile.Library,
                TotalParts = flow.Parts.Count,
                CurrentPart = 0,
                CurrentPartPercent = 0,
                CurrentPartName = string.Empty,
                StartedAt = DateTime.UtcNow,
                WorkingFile = workingFile,
                IsDirectory = lib.Folders,
                LibraryPath = lib.Path, 
                Fingerprint = lib.UseFingerprinting,
                InitialSize = lib.Folders ? GetDirectorySize(workingFile) : new FileInfo(workingFile).Length
            };

            LogInfo("Initial Size: " + info.InitialSize);  

            var runner = new Runner(info, flow, node, workingDir);
            runner.Run();
        }


        private static long GetDirectorySize(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                return dir.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(x => x.Length);
            }
            catch (Exception ex)
            {
                LogInfo("Failed retrieving directory size: " + ex.Message);
                return 0;
            }
        }


        internal static void LogInfo(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + " - INFO -> " + message);
        }
    }
}