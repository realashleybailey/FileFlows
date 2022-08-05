using System.Text.RegularExpressions;
using FileFlows.Server;
using Microsoft.Extensions.Logging;

namespace FileFlows.FlowRunner;

using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// A runner instance, this is called as a standalone application that is fired up when FileFlows needs to process a file
/// it exits when done, free up any resources used by this process
/// </summary>
public class Runner
{
    private FlowExecutorInfo Info;
    private Flow Flow;
    private ProcessingNode Node;
    private CancellationTokenSource CancellationToken = new CancellationTokenSource();
    private bool Canceled = false;
    private string WorkingDir;
    private string ScriptDir, ScriptSharedDir, ScriptFlowDir;

    /// <summary>
    /// Creates an instance of a Runner
    /// </summary>
    /// <param name="info">The execution info that is be run</param>
    /// <param name="flow">The flow that is being executed</param>
    /// <param name="node">The processing node that is executing this flow</param>
    /// <param name="workingDir">the temporary working directory to use</param>
    public Runner(FlowExecutorInfo info, Flow flow, ProcessingNode node, string workingDir)
    {
        this.Info = info;
        this.Flow = flow;
        this.Node = node;
        this.WorkingDir = workingDir;
    }

    /// <summary>
    /// A delegate for the flow complete event
    /// </summary>
    public delegate void FlowCompleted(Runner sender, bool success);
    /// <summary>
    /// An event that is called when the flow completes
    /// </summary>
    public event FlowCompleted OnFlowCompleted;
    private NodeParameters nodeParameters;

    private Node CurrentNode;

    private void RecordNodeExecution(string nodeName, string nodeUid, int output, TimeSpan duration, FlowPart part)
    {
        if (Info.LibraryFile == null)
            return;

        Info.LibraryFile.ExecutedNodes ??= new List<ExecutedNode>();
        Info.LibraryFile.ExecutedNodes.Add(new ExecutedNode
        {
            NodeName = nodeName,
            NodeUid = part.Type == FlowElementType.Script ? "ScriptNode" : nodeUid,
            Output = output,
            ProcessingTime = duration,
        });
    }

    /// <summary>
    /// Starts the flow runner processing
    /// </summary>
    public void Run()
    {
        var systemHelper = new SystemHelper();
        try
        {
            systemHelper.Start();
            var service = FlowRunnerService.Load();
            var updated = service.Start(Info).Result;
            if (updated == null)
                return; // failed to update
            var communicator = FlowRunnerCommunicator.Load(Info.LibraryFile.Uid);
            communicator.OnCancel += Communicator_OnCancel;
            bool finished = false;
            var task = Task.Run(async () =>
            {
                while (finished == false)
                {
                    if (finished == false)
                    {
                        bool success = await communicator.Hello(Program.Uid);
                        if (success == false)
                        {
                            Communicator_OnCancel();
                            return;
                        }
                    }
                    await Task.Delay(5_000);
                }
            });
            try
            {
                RunActual(communicator);
            }
            catch(Exception ex)
            {
                finished = true;
                task.Wait();
                
                if (Info.LibraryFile?.Status == FileStatus.Processing)
                    Info.LibraryFile.Status = FileStatus.ProcessingFailed;
                
                nodeParameters?.Logger?.ELog("Error in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                throw;
            }
            finally
            {
                finished = true;
                task.Wait();
                communicator.OnCancel -= Communicator_OnCancel;
                communicator.Close();
            }
        }
        finally
        {
            Finish().Wait();
            systemHelper.Stop();
        }
    }

    private void Communicator_OnCancel()
    {
        nodeParameters?.Logger?.ILog("##### CANCELING FLOW!");
        CancellationToken.Cancel();
        nodeParameters?.Cancel();
        Canceled = true;
        if (CurrentNode != null)
            CurrentNode.Cancel().Wait();
    }

    public async Task Finish()
    {
        if (nodeParameters?.Logger is FlowLogger fl)
            Info.Log = fl.ToString();

        if(nodeParameters.OriginalMetadata != null)
            Info.LibraryFile.OriginalMetadata = nodeParameters.OriginalMetadata;

        await Complete();
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    private void CalculateFinalSize()
    {
        if (nodeParameters.IsDirectory)
            Info.LibraryFile.FinalSize = nodeParameters.GetDirectorySize(nodeParameters.WorkingFile);
        else
        {
            Info.LibraryFile.FinalSize = nodeParameters.LastValidWorkingFileSize;

            try
            {
                if (Info.Fingerprint)
                {
                    Info.LibraryFile.Fingerprint = ServerShared.Helpers.FileHelper.CalculateFingerprint(nodeParameters.WorkingFile) ?? string.Empty;
                    nodeParameters?.Logger?.ILog("Final Fingerprint: " + Info.LibraryFile.Fingerprint);
                }
                else
                {
                    Info.LibraryFile.Fingerprint = string.Empty;
                }
            }
            catch (Exception ex)
            {
                nodeParameters?.Logger?.ILog("Error with fingerprinting: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        nodeParameters?.Logger?.ILog("Original Size: " + Info.LibraryFile.OriginalSize);
        nodeParameters?.Logger?.ILog("Final Size: " + Info.LibraryFile.FinalSize);
        Info.LibraryFile.OutputPath = Node.UnMap(nodeParameters.WorkingFile);
        nodeParameters?.Logger?.ILog("Output Path: " + Info.LibraryFile.OutputPath);
        nodeParameters?.Logger?.ILog("Final Status: " + Info.LibraryFile.Status);

    }

    private async Task Complete()
    {
        DateTime start = DateTime.Now;
        do
        {
            try
            {
                CalculateFinalSize();

                var service = FlowRunnerService.Load();
                await service.Complete(Info);
                return;
            }
            catch (Exception) { }
            await Task.Delay(30_000);
        } while (DateTime.Now.Subtract(start) < new TimeSpan(0, 10, 0));
        Logger.Instance?.ELog("Failed to inform server of flow completion");
    }

    private void StepChanged(int step, string partName)
    {
        Info.CurrentPartName = partName;
        Info.CurrentPart = step;
        try
        {
            var service = FlowRunnerService.Load();
            service.Update(Info);
        }
        catch (Exception) 
        { 
            // silently fail, not a big deal, just incremental progress update
        }
    }

    private void UpdatePartPercentage(float percentage)
    {
        float diff = Math.Abs(Info.CurrentPartPercent - percentage);
        if (diff < 0.1)
            return; // so small no need to tell server about update;

        Info.CurrentPartPercent = percentage;

        try 
        { 
            var service = FlowRunnerService.Load();
            service.Update(Info);
        }
        catch (Exception)
        {
            // silently fail, not a big deal, just incremental progress update
        }
    }

    private void SetStatus(FileStatus status)
    {
        DateTime start = DateTime.Now;
        Info.LibraryFile.Status = status;
        if (status == FileStatus.Processed)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.Now;
        }
        else if(status == FileStatus.ProcessingFailed)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.Now;
        }
        do
        {
            try
            {
                var service = FlowRunnerService.Load();
                CalculateFinalSize();
                service.Update(Info);
                Logger.Instance?.DLog("Set final status to: " + status);
                return;
            }
            catch (Exception ex)
            {
                // this is more of a problem, its not ideal, so we do try again
                Logger.Instance?.WLog("Failed to set status on server: " + ex.Message);
            }
            Thread.Sleep(5_000);
        } while (DateTime.Now.Subtract(start) < new TimeSpan(0, 3, 0));
    }

    private void RunActual(IFlowRunnerCommunicator communicator)
    {
        nodeParameters = new NodeParameters(Node.Map(Info.LibraryFile.Name), new FlowLogger(communicator), Info.IsDirectory, Info.LibraryPath);
        nodeParameters.PathMapper = (string path) => Node.Map(path);
        nodeParameters.PathUnMapper = (string path) => Node.UnMap(path);
        nodeParameters.ScriptExecutor = new FileFlows.ServerShared.ScriptExecutor()
        {
            SharedDirectory = ScriptSharedDir,
            FileFlowsUrl = Service.ServiceBaseUrl
        };
        var systemVariables = new VariableService().GetAll().Result?.ToArray() ?? new Variable []{ };
        foreach (var variable in systemVariables)
        {
            if (nodeParameters.Variables.ContainsKey(variable.Name) == false)
                nodeParameters.Variables.Add(variable.Name, variable.Value);
        }

        FileHelper.DontChangeOwner = Node.DontChangeOwner;
        FileHelper.DontSetPermissions = Node.DontSetPermissions;
        FileHelper.Permissions = Node.Permissions;

        List<Guid> runFlows = new List<Guid>();
        runFlows.Add(Flow.Uid);

        Info.LibraryFile.OriginalSize = nodeParameters.IsDirectory ? nodeParameters.GetDirectorySize(nodeParameters.WorkingFile) : new FileInfo(nodeParameters.WorkingFile).Length;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;
        Shared.Helpers.HttpHelper.Logger = nodeParameters.Logger;

        nodeParameters.Logger!.ILog("File: " + nodeParameters.FileName);
        nodeParameters.Logger!.ILog("Executing Flow: " + Flow.Name);

        DownloadPlugins();
        DownloadScripts();

        nodeParameters.Result = NodeResult.Success;
        nodeParameters.GetToolPathActual = (string name) =>
        {
            var nodeService = NodeService.Load();
            return Node.Map(nodeService.GetVariable(name).Result);
        };
        nodeParameters.GetPluginSettingsJson = (string pluginSettingsType) =>
        {
            var pluginService = PluginService.Load();
            return pluginService.GetSettingsJson(pluginSettingsType).Result;
        };
        nodeParameters.StatisticRecorder = (string name, object value) =>
        {
            var statService = StatisticService.Load();
            statService.Record(name, value);
        };

        var pluginLoader = PluginService.Load();
        var status = ExecuteFlow(Flow, pluginLoader, runFlows);
        SetStatus(status);
        if(status == FileStatus.ProcessingFailed && Canceled == false)
        {
            // try run FailureFlow
            var fs = new FlowService();
            var failureFlow = fs.GetFailureFlow(Info.Library.Uid).Result;
            if (failureFlow != null)
            {
                nodeParameters.UpdateVariables(new Dictionary<string, object>
                {
                    { "FailedNode", CurrentNode?.Name },
                    { "FlowName", Flow.Name }
                });
                ExecuteFlow(failureFlow, pluginLoader, runFlows, failure: true);
            }
        }
    }

    private FileStatus ExecuteFlow(Flow flow, IPluginService pluginLoader, List<Guid> runFlows, bool failure = false)
    { 
        int count = 0;
        ObjectReference? gotoFlow = null;
        nodeParameters.GotoFlow = (flow) =>
        {
            if (runFlows.Contains(flow.Uid))
                throw new Exception($"Flow '{flow.Uid}' ['{flow.Name}'] has already been executed, cannot link to existing flow as this could cause an infinite loop.");
            gotoFlow = flow;
        };

        // find the first node
        var part = flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
        if (part == null)
        {
            nodeParameters.Logger!.ELog("Failed to find Input node");
            return FileStatus.ProcessingFailed;
        }

        int step = 0;
        StepChanged(step, part.Name);

        // need to clear this incase the file is being reprocessed
        if(failure == false)
            Info.LibraryFile.ExecutedNodes = new List<ExecutedNode>();

        while (count++ < 50)
        {
            if (CancellationToken.IsCancellationRequested || Canceled)
            {
                nodeParameters.Logger?.WLog("Flow was canceled");
                nodeParameters.Result = NodeResult.Failure;
                return FileStatus.ProcessingFailed;
            }
            if (part == null)
            {
                nodeParameters.Logger?.WLog("Flow part was null");
                nodeParameters.Result = NodeResult.Failure;
                return FileStatus.ProcessingFailed;
            }

            try
            {

                CurrentNode = LoadNode(part!);

                if (CurrentNode == null)
                {
                    // happens when canceled    
                    nodeParameters.Logger?.ELog("Failed to load node: " + part.Name);                    
                    nodeParameters.Result = NodeResult.Failure;
                    return FileStatus.ProcessingFailed;
                }
                ++step;
                StepChanged(step, CurrentNode.Name);

                nodeParameters.Logger?.ILog(new string('=', 70));
                nodeParameters.Logger?.ILog($"Executing Node {(Info.LibraryFile.ExecutedNodes.Count + 1)}: {part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? CurrentNode.Name} [{CurrentNode.GetType().FullName}]");
                nodeParameters.Logger?.ILog(new string('=', 70));

                gotoFlow = null; // clear it, in case this node requests going to a different flow
                
                DateTime nodeStartTime = DateTime.Now;
                int output = 0;
                try
                {
                    if (CurrentNode.PreExecute(nodeParameters) == false)
                        throw new Exception("PreExecute failed");
                    output = CurrentNode.Execute(nodeParameters);
                }
                catch(Exception)
                {
                    output = -1;
                    throw;
                }
                finally
                {
                    TimeSpan executionTime = DateTime.Now.Subtract(nodeStartTime);
                    if(failure == false)
                        RecordNodeExecution(part.Label?.EmptyAsNull() ?? part.Name?.EmptyAsNull() ?? CurrentNode.Name, part.FlowElementUid, output, executionTime, part);
                    nodeParameters.Logger?.ILog("Node execution time: " + executionTime);
                    nodeParameters.Logger?.ILog(new string('=', 70));
                }

                if (gotoFlow != null)
                {
                    var fs = new FlowService();
                    var newFlow = fs.Get(gotoFlow.Uid).Result;
                    if (newFlow == null)
                    {
                        nodeParameters.Logger?.ELog("Unable goto flow with UID:" + gotoFlow.Uid + " (" + gotoFlow.Name + ")");
                        nodeParameters.Result = NodeResult.Failure;
                        return FileStatus.ProcessingFailed;
                    }
                    flow = newFlow;

                    nodeParameters.Logger?.ILog("Changing flows to: " + newFlow.Name);
                    this.Flow = newFlow;
                    runFlows.Add(gotoFlow.Uid);

                    // find the first node
                    part = flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
                    if (part == null)
                    {
                        nodeParameters.Logger!.ELog("Failed to find Input node");
                        return FileStatus.ProcessingFailed;
                    }
                    Info.TotalParts = flow.Parts.Count;
                    step = 0;
                }
                else
                {
                    nodeParameters.Logger?.DLog("output: " + output);
                    if (output == -1)
                    {
                        // the execution failed                     
                        nodeParameters.Logger?.ELog("node returned error code:", CurrentNode!.Name);
                        nodeParameters.Result = NodeResult.Failure;
                        return FileStatus.ProcessingFailed;
                    }
                    var outputNode = part.OutputConnections?.Where(x => x.Output == output)?.FirstOrDefault();
                    if (outputNode == null)
                    {
                        nodeParameters.Logger?.DLog("Flow completed");
                        // flow has completed
                        nodeParameters.Result = NodeResult.Success;
                        nodeParameters.Logger?.DLog("File status set to processed");
                        return FileStatus.Processed;
                    }

                    var newPart = outputNode == null ? null : flow.Parts.Where(x => x.Uid == outputNode.InputNode).FirstOrDefault();
                    if (newPart == null)
                    {
                        // couldn't find the connection, maybe bad data, but flow has now finished
                        nodeParameters.Logger?.WLog("Couldn't find output node, flow completed: " + outputNode?.Output);
                        return FileStatus.Processed;
                    }

                    part = newPart;
                }
            }
            catch (Exception ex)
            {
                nodeParameters.Result = NodeResult.Failure;
                nodeParameters.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                Logger.Instance?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                return FileStatus.ProcessingFailed;
            }
        }
        nodeParameters.Logger?.ELog("Too many nodes in flow, processing aborted");
        return FileStatus.ProcessingFailed;
    }

    private void DownloadScripts()
    {
        var service = ScriptService.Load();
        DateTime start = DateTime.Now;
        if (Directory.Exists(nodeParameters.TempPath) == false)
            Directory.CreateDirectory(nodeParameters.TempPath);

        ScriptDir = Path.Combine(nodeParameters.TempPath, "Scripts");
        if (Directory.Exists(ScriptDir) == false)
            Directory.CreateDirectory(ScriptDir);
        
        ScriptSharedDir = Path.Combine(ScriptDir, "Shared");
        if (Directory.Exists(ScriptSharedDir) == false)
            Directory.CreateDirectory(ScriptSharedDir);
        
        ScriptFlowDir = Path.Combine(ScriptDir, "Flow");
        if (Directory.Exists(ScriptFlowDir) == false)
            Directory.CreateDirectory(ScriptFlowDir);

        var allScripts = service.GetScripts().Result?.ToArray() ?? new Script[] { };
        var shared = allScripts.Where(x => x.Type == ScriptType.Shared);
        foreach (var script in shared)
        {
            File.WriteAllText(Path.Combine(ScriptSharedDir, script.Name + ".js"), script.Code);
        }

        var flowScripts = allScripts.Where(x => x.Type == ScriptType.Flow);
        foreach (var script in flowScripts)
        {
            File.WriteAllText(Path.Combine(ScriptFlowDir, script.Name + ".js"), script.Code);
        }
        
        TimeSpan timeTaken = DateTime.Now - start;
        nodeParameters.Logger?.ILog("Time taken to download scripts: " + timeTaken.ToString());
    }
    
    private void DownloadPlugins()
    {
        var service = PluginService.Load();
        var plugins = service.GetAll().Result;
        DateTime start = DateTime.Now;
        List<Task<bool>> tasks = new List<Task<bool>>();
        foreach (var plugin in plugins)
        {
            tasks.Add(DownloadPlugin(service, plugin));
        }

        Task.WaitAll(tasks.ToArray());
        TimeSpan timeTaken = DateTime.Now - start;
        nodeParameters.Logger?.ILog("Time taken to download plugins: " + timeTaken.ToString());
    }


    private async Task<bool> DownloadPlugin(IPluginService service, PluginInfo plugin)
    {
        try
        {
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool macOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool is64bit = IntPtr.Size == 8;

            DateTime dtDownload = DateTime.Now;
            nodeParameters.Logger?.ILog($"Plugin: {plugin.PackageName} ({plugin.Version})");
            if (Directory.Exists(nodeParameters.TempPath) == false)
                Directory.CreateDirectory(nodeParameters.TempPath);
            string file = Path.Combine(nodeParameters.TempPath, $"{plugin.PackageName}.ffplugin");
            var data = await service.Download(plugin);
            if (data == null || data.Length == 0)
            {
                nodeParameters.Logger?.ELog("Failed to download plugin: " + plugin.PackageName);
                return false;
            }

            string destDir = Path.Combine(nodeParameters.TempPath, plugin.PackageName);

            FileHelper.CreateDirectoryIfNotExists(nodeParameters.Logger, destDir);

            FileHelper.SaveFile(nodeParameters.Logger, file, data);
            
            nodeParameters.Logger?.ILog($"Time taken to download plugin '{plugin.PackageName}': " + (DateTime.Now.Subtract(dtDownload)));

            DateTime dtExtract = DateTime.Now;
            FileHelper.ExtractFile(nodeParameters.Logger, file, destDir);
            File.Delete(file);

            // check if there are runtime specific files that need to be moved
            foreach (string rdir in windows ? new[] { "win", "win-" + (is64bit ? "x64" : "x86") } : macOs ? new[] { "osx-x64" } : new string[] { "linux-x64" })
            {
                var runtimeDir = new DirectoryInfo(Path.Combine(destDir, "runtimes", rdir));
                nodeParameters.Logger?.ILog("Searching for runtime directory: " + runtimeDir.FullName);
                if (runtimeDir.Exists)
                {
                    foreach (var dll in runtimeDir.GetFiles("*.dll", SearchOption.AllDirectories))
                    {
                        try
                        {

                            nodeParameters.Logger?.ILog("Trying to move file: \"" + dll.FullName + "\" to \"" + destDir + "\"");
                            dll.MoveTo(Path.Combine(destDir, dll.Name));
                            nodeParameters.Logger?.ILog("Moved file: \"" + dll.FullName + "\" to \"" + destDir + "\"");
                        }
                        catch (Exception ex)
                        {
                            nodeParameters.Logger?.ILog("Failed to move file: " + ex.Message);
                        }
                    }
                }
            }
            nodeParameters.Logger?.ILog($"Time taken to extract plugin '{plugin.PackageName}': " + (DateTime.Now.Subtract(dtExtract)));

            return true;
        }
        catch (Exception ex)
        {
            nodeParameters.Logger.ILog($"Failed downloading pugin '{plugin.Name}': " + ex.Message);
            return false;
        }
    }

    private Type? GetNodeType(string fullName)
    {
        foreach (var dll in new DirectoryInfo(WorkingDir).GetFiles("*.dll", SearchOption.AllDirectories))
        {
            try
            {
                //var assembly = Context.LoadFromAssemblyPath(dll.FullName);
                var assembly = Assembly.LoadFrom(dll.FullName);
                var types = assembly.GetTypes();
                var pluginType = types.Where(x => x.IsAbstract == false && x.FullName == fullName).FirstOrDefault();
                if (pluginType != null)
                    return pluginType;
            }
            catch (Exception) { }
        }
        return null;
    }

    private Node LoadNode(FlowPart part)
    {
        if (part.Type == FlowElementType.Script)
        {
            // special type
            var nodeScript = new ScriptNode();
            nodeScript.Model = part.Model;
            string scriptName = part.FlowElementUid[7..]; // 7 to remove "Scripts." 
            nodeScript.Code = GetScriptCode(scriptName);
            if (string.IsNullOrEmpty(nodeScript.Code))
                throw new Exception("Script not found");
            
            if(string.IsNullOrWhiteSpace(part.Name))
                part.Name = scriptName;
            return nodeScript;
        }
        
        var nt = GetNodeType(part.FlowElementUid);
        if (nt == null)
            return new Node();
        var node = Activator.CreateInstance(nt);
        if (part.Model is IDictionary<string, object> dict)
        {
            foreach (var k in dict.Keys)
            {
                try
                {
                    if (k == "Name")
                        continue; // this is just the display name in the flow UI
                    var prop = nt.GetProperty(k, BindingFlags.Instance | BindingFlags.Public);
                    if (prop == null)
                        continue;

                    if (dict[k] == null)
                        continue;

                    var value = FileFlows.Shared.Converter.ConvertObject(prop.PropertyType, dict[k]);
                    if (value != null)
                        prop.SetValue(node, value);
                }
                catch (Exception ex)
                {
                    Logger.Instance?.ELog("Failed setting property: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    Logger.Instance?.ELog("Type: " + nt.Name + ", Property: " + k);
                }
            }
        }
        if(node == null)
            return default;
        return (Node)node;

    }

    /// <summary>
    /// Loads the code for a script
    /// </summary>
    /// <param name="scriptName">the name of the script</param>
    /// <returns>the code of the script</returns>
    private string GetScriptCode(string scriptName)
    {
        if (scriptName.EndsWith(".js") == false)
            scriptName += ".js";
        var file = new FileInfo(Path.Combine(ScriptFlowDir, scriptName));
        if (file.Exists == false)
            return string.Empty;
        return File.ReadAllText(file.FullName);
    }
}
