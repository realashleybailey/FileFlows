using System.Threading;

namespace FileFlows.Node.Workers;

using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;


/// <summary>
/// A flow worker executes a flow and start a flow runner
/// </summary>
public class FlowWorker : Worker
{
    /// <summary>
    /// A unique identifier to identify the flow worker 
    /// </summary>
    public readonly Guid Uid = Guid.NewGuid();

    /// <summary>
    /// The instance of the flow worker 
    /// </summary>
    private static FlowWorker Instance;

    private readonly Mutex mutex = new Mutex();
    private readonly List<Guid> ExecutingRunners = new ();

    
    /// <summary>
    /// If this flow worker is running on the server or an external processing node
    /// </summary>
    private readonly bool isServer;

    private bool FirstExecute;

    /// <summary>
    /// The host name of the processing node
    /// </summary>
    private string Hostname { get; set; }

    
    /// <summary>
    /// Constructs a flow worker instance
    /// </summary>
    /// <param name="hostname">the host name of the processing node</param>
    /// <param name="isServer">if this flow worker is running on the server or an external processing node</param>
    public FlowWorker(string hostname, bool isServer = false) : base(ScheduleType.Second, 10)
    {
        FlowWorker.Instance = this;
        this.isServer = isServer;
        this.FirstExecute = true;
        this.Hostname = hostname;
    }

    /// <summary>
    /// A function to check if this flow worker is enabled
    /// </summary>
    public Func<bool>? IsEnabledCheck { get; set; }

    /// <summary>
    /// Gets if there are any active runners
    /// </summary>
    public static bool HasActiveRunners => Instance?.ExecutingRunners?.Any() == true;


    /// <summary>
    /// Executes the flow worker and start a flow runner if required
    /// </summary>
    protected override void Execute()
    {
        if (UpdaterWorker.UpdatePending)
            return;
        
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

        if (node == null)
        {
            Logger.Instance?.DLog($"Node not found");
            return;
        }

        string nodeName = node?.Name == "FileFlowsServer" ? "Internal Processing Node" : (node?.Name ?? "Unknown");

        if (node?.Enabled != true)
        {
            Logger.Instance?.DLog($"Node '{nodeName}' is not enabled");
            return;
        }

        if(string.IsNullOrEmpty(node?.Schedule) == false && TimeHelper.InSchedule(node.Schedule) == false)
        {
            Logger.Instance?.DLog($"Node '{nodeName}' is out of schedule");
            return;
        }

        if (node?.FlowRunners <= ExecutingRunners.Count)
        {
            Logger.Instance?.DLog($"At limit of running executors on '{nodeName}': " + node.FlowRunners);
            return; // already maximum executors running
        }


        string tempPath = node?.TempPath ?? string.Empty;
        if (string.IsNullOrEmpty(tempPath))
        {
            Logger.Instance?.ELog($"Temp Path not set on node '{nodeName}', cannot process");
            return;
        }
        
        if(Directory.Exists(tempPath) == false)
        {
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception)
            {
                Logger.Instance?.ELog($"Temp Path does not exist on on node '{nodeName}', and failed to create it: {tempPath}");
                return;
            }
        }
        
        var libFileService = LibraryFileService.Load();
        var libFile = libFileService.GetNext(node?.Name ?? string.Empty, node?.Uid ?? Guid.Empty, Uid).Result;
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
                        process.StartInfo.FileName = GetDotnetLocation();
                        process.StartInfo.WorkingDirectory = DirectoryHelper.FlowRunnerDirectory;
                        process.StartInfo.ArgumentList.Add("FileFlows.FlowRunner.dll");
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


    /// <summary>
    /// Adds an executing runner to the list of currently running flow runners
    /// </summary>
    /// <param name="uid">The uid of the flow runner to add</param>
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

    
    /// <summary>
    /// Removes a flow runner from the list of currently executing flow runners
    /// </summary>
    /// <param name="uid">The uid of the flow runner to remove</param>
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
                Logger.Instance?.ILog("Executing runner not in list: " + uid +" => " + string.Join(",", ExecutingRunners.Select(x => x.ToString())));
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

    /// <summary>
    /// Saves the flow runner log to the server
    /// </summary>
    /// <param name="libFile">The Library File that was processed</param>
    /// <param name="log">The full flow runner log</param>
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


    /// <summary>
    /// The location of dotnet
    /// </summary>
    private static string Dotnet = "";
    
    /// <summary>
    /// Gets the location of dotnet to use to start the flow runner
    /// </summary>
    /// <returns>the location of dotnet to use to start the flow runner</returns>
    private string GetDotnetLocation()
    {
        if(string.IsNullOrEmpty(Dotnet))
        {
            if (Globals.IsWindows == false && File.Exists("/root/.dotnet/dotnet"))
                Dotnet = "/root/.dotnet/dotnet"; // location of docker
            else
                Dotnet = "dotnet";// assume in PATH
        }
        return Dotnet;
    }
}
