using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileFlows.FlowRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                args ??= new string[] { };
                bool server = args.Any(x => x.ToLower() == "--server");

                string uid = GetArgument(args, "--uid");
                if (string.IsNullOrEmpty(uid))
                    throw new Exception("uid not set.");

                string tempPath = GetArgument(args, "--tempPath");
                if (string.IsNullOrEmpty(tempPath) || Directory.Exists(tempPath) == false)
                    throw new Exception("Temp path doesnt exist: " + tempPath);

                string baseUrl = GetArgument(args, "--baseUrl");
                if (string.IsNullOrEmpty(baseUrl))
                    throw new Exception("baseUrl not set");
                Console.WriteLine("Base URL: " + baseUrl);
                Service.ServiceBaseUrl = baseUrl;

                string workingDir = Path.Combine(tempPath, "Runner-" + uid);
                Directory.CreateDirectory(workingDir);

                var libfileUid = Guid.Parse(GetArgument(args, "--libfile"));
                Shared.Helpers.HttpHelper.Client = new HttpClient();
                Execute(server, tempPath, libfileUid, workingDir);
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                while(ex.InnerException != null)
                {
                    Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    ex = ex.InnerException;
                }
                return;
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


        static void Execute(bool isServer, string tempPath, Guid libfileUid, string workingDir)
        {
            ProcessingNode node;
            var nodeService = NodeService.Load();
            try
            {
                string address = isServer ? "INTERNAL_NODE" : Environment.MachineName;
                Console.WriteLine("Address: "+ address);
                var nodeTask = nodeService.GetByAddress(address);
                Console.WriteLine("Waiting on node task");
                nodeTask.Wait();
                Console.WriteLine("Completed node task");
                node = nodeTask.Result;
                if (node == null)
                    throw new Exception("Failed to load node!!!!");
                Console.WriteLine("Node SignalrUrl: " + node.SignalrUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to register node: " + ex.Message + Environment.NewLine + ex.StackTrace);
                throw;
            }

            FlowRunnerCommunicator.SignalrUrl = node.SignalrUrl;

            var libFileService = LibraryFileService.Load();
            var libFile = libFileService.Get(libfileUid).Result;
            if (libFile == null)
                return; // nothing to process

            string workingFile = node.Map(libFile.Name);

            var libfileService = LibraryFileService.Load();
            var libService = LibraryService.Load();
            var lib = libService.Get(libFile.Library.Uid).Result;
            if (lib == null)
            {
                libfileService.Delete(libFile.Uid).Wait();
                return;
            }

            var flowService = FlowService.Load();
            FileSystemInfo file = lib.Folders ? new DirectoryInfo(workingFile) : new FileInfo(workingFile);
            if (file.Exists == false)
            {
                libfileService.Delete(libFile.Uid).Wait();
                return;
            }

            var flow = flowService.Get(lib.Flow?.Uid ?? Guid.Empty).Result;
            if (flow == null || flow.Uid == Guid.Empty)
            {
                libFile.Status = FileStatus.FlowNotFound;
                libfileService.Update(libFile).Wait();
                return;
            }

            // update the library file to reference the updated flow (if changed)
            if (libFile.Flow.Name != flow.Name || libFile.Flow.Uid != flow.Uid)
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
                LibraryPath = lib.Path
            };

            var runner = new Runner(info, flow, node, workingDir);
            runner.Run();
        }

    }
}