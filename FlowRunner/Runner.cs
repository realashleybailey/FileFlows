namespace FileFlows.FlowRunner;

using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Reflection;
using System.Runtime.InteropServices;

public class Runner
{
    private FlowExecutorInfo Info;
    private Flow Flow;
    private ProcessingNode Node;
    private CancellationTokenSource CancellationToken = new CancellationTokenSource();
    private bool Canceled = false;
    private string WorkingDir;

    public Runner(FlowExecutorInfo info, Flow flow, ProcessingNode node, string workingDir)
    {
        this.Info = info;
        this.Flow = flow;
        this.Node = node;
        this.WorkingDir = workingDir;
    }

    public delegate void FlowCompleted(Runner sender, bool success);
    public event FlowCompleted OnFlowCompleted;
    private NodeParameters nodeParameters;
    private Node currentNode;

    public void Run()
    {
        try
        {
            var service = FlowRunnerService.Load();
            var updated = service.Start(Info).Result;
            if (updated == null)
                return; // failed to update
            Info.Uid = updated.Uid;
            var communicator = FlowRunnerCommunicator.Load(Info.LibraryFile.Uid);
            communicator.OnCancel += Communicator_OnCancel;
            try
            {
                RunActual(communicator);
            }
            catch(Exception ex)
            {
                nodeParameters?.Logger?.ELog("Error in runner: " + ex.Message + Environment.NewLine + ex.StackTrace);
                throw;
            }
            finally
            {
                communicator.OnCancel -= Communicator_OnCancel;
                communicator.Close();
            }
        }
        finally
        {
            Finish().Wait();
        }
    }

    private void Communicator_OnCancel()
    {
        nodeParameters?.Logger?.ILog("##### CANCELING FLOW!");
        CancellationToken.Cancel();
        Canceled = true;
        if (currentNode != null)
            currentNode.Cancel().Wait();
    }

    public async Task Finish()
    {
        if (nodeParameters?.Logger is FlowLogger fl)
            Info.Log = fl.ToString();

        await Complete();
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    private void CalculateFinalSize()
    {
        Info.LibraryFile.FinalSize = nodeParameters.IsDirectory ? nodeParameters.GetDirectorySize(nodeParameters.WorkingFile) : new FileInfo(nodeParameters.WorkingFile).Length;
        nodeParameters?.Logger?.ILog("Original Size: " + Info.LibraryFile.OriginalSize);
        nodeParameters?.Logger?.ILog("Final Size: " + Info.LibraryFile.FinalSize);
        Info.LibraryFile.OutputPath = Node.UnMap(nodeParameters.WorkingFile);
        nodeParameters?.Logger?.ILog("Output Path: " + Info.LibraryFile.OutputPath);

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
            nodeParameters?.Logger?.ILog("Error with fingerpriting: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
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

        try { 
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
        if(status == FileStatus.Processed)
        {
            CalculateFinalSize();
            Info.LibraryFile.ProcessingEnded = DateTime.UtcNow;
        }
        else if(status == FileStatus.ProcessingFailed)
        {
            Info.LibraryFile.ProcessingEnded = DateTime.UtcNow;
        }
        do
        {
            try
            {
                var service = FlowRunnerService.Load();
                service.Update(Info);
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
        List<Guid> runFlows = new List<Guid>();
        runFlows.Add(Flow.Uid);

        ObjectReference gotoFlow = null;
        nodeParameters.GotoFlow = (flow) =>
        {
            if (runFlows.Contains(flow.Uid))
                throw new Exception($"Flow '{flow.Uid}' ['{flow.Name}'] has already been executed, cannot link to existing flow as this could cause an infinite loop.");
            gotoFlow = flow;
        };
        Info.LibraryFile.OriginalSize = nodeParameters.IsDirectory ? nodeParameters.GetDirectorySize(nodeParameters.WorkingFile) : new FileInfo(nodeParameters.WorkingFile).Length;
        nodeParameters.TempPath = WorkingDir;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;
        Shared.Helpers.HttpHelper.Logger = nodeParameters.Logger;

        nodeParameters.Logger!.ILog("File: " + nodeParameters.FileName);
        nodeParameters.Logger!.ILog("Excecuting Flow: " + Flow.Name);

        DownloadPlugins();

        nodeParameters.Result = NodeResult.Success;
        nodeParameters.GetToolPathActual = (string name) =>
        {
            var nodeService = NodeService.Load();
            return Node.Map(nodeService.GetToolPath(name).Result);
        };
        nodeParameters.GetPluginSettingsJson = (string pluginSettingsType) =>
        {
            var pluginService = PluginService.Load();
            return pluginService.GetSettingsJson(pluginSettingsType).Result;
        };

        int count = 0;

        // find the first node
        var part = Flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
        if (part == null)
        {
            nodeParameters.Logger!.ELog("Failed to find Input node");
            SetStatus(FileStatus.ProcessingFailed);
            return;
        }

        int step = 0;
        StepChanged(step, part.Name);
        var pluginLoader = PluginService.Load();

        while (count++ < 50)
        {
            if (CancellationToken.IsCancellationRequested || Canceled)
            {
                nodeParameters.Logger?.WLog("Flow was canceled");
                nodeParameters.Result = NodeResult.Failure;
                SetStatus(FileStatus.ProcessingFailed);
                return;
            }
            if (part == null)
            {
                nodeParameters.Logger?.WLog("Flow part was null");
                nodeParameters.Result = NodeResult.Failure;
                SetStatus(FileStatus.ProcessingFailed);
                return;
            }

            try
            {

                nodeParameters.Logger?.DLog("Executing part:" + (part!.Name?.EmptyAsNull() ?? part!.GetType().FullName ?? "unknown"));
                currentNode = LoadNode(part!);

                if (currentNode == null)
                {
                    // happens when canceled    
                    nodeParameters.Logger?.ELog("Failed to load node: " + part.Name);
                    SetStatus(FileStatus.ProcessingFailed);
                    nodeParameters.Result = NodeResult.Failure;
                    return;
                }
                ++step;
                StepChanged(step, currentNode.Name);

                nodeParameters.Logger?.DLog("node: " + currentNode.Name);
                gotoFlow = null; // clear it, incase this node requests going to a different flow
                int output = currentNode.Execute(nodeParameters);

                if (gotoFlow != null)
                {
                    var fs = new FlowService();
                    var newFlow = fs.Get(gotoFlow.Uid).Result;
                    if (newFlow == null)
                    {
                        nodeParameters.Logger?.ELog("Unable goto flow with UID:" + gotoFlow.Uid + " (" + gotoFlow.Name + ")");
                        nodeParameters.Result = NodeResult.Failure;
                        SetStatus(FileStatus.ProcessingFailed);
                        return;
                    }

                    nodeParameters.Logger?.ILog("Changing flows to: " + newFlow.Name);
                    this.Flow = newFlow;

                    // find the first node
                    part = Flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
                    if (part == null)
                    {
                        nodeParameters.Logger!.ELog("Failed to find Input node");
                        SetStatus(FileStatus.ProcessingFailed);
                        return;
                    }
                    Info.TotalParts = Flow.Parts.Count;
                    step = 0;
                }
                else
                {
                    nodeParameters.Logger?.DLog("output: " + output);
                    if (output == -1)
                    {
                        // the execution failed                     
                        nodeParameters.Logger?.ELog("node returned error code:", currentNode!.Name);
                        nodeParameters.Result = NodeResult.Failure;
                        SetStatus(FileStatus.ProcessingFailed);
                        return;
                    }
                    var outputNode = part.OutputConnections?.Where(x => x.Output == output)?.FirstOrDefault();
                    if (outputNode == null)
                    {
                        nodeParameters.Logger?.DLog("flow completed");
                        // flow has completed
                        nodeParameters.Result = NodeResult.Success;
                        SetStatus(FileStatus.Processed);
                        nodeParameters.Logger?.DLog("flow status set to processed");
                        return;
                    }

                    part = outputNode == null ? null : Flow.Parts.Where(x => x.Uid == outputNode.InputNode).FirstOrDefault();
                    if (part == null)
                    {
                        // couldnt find the connection, maybe bad data, but flow has now finished
                        nodeParameters.Logger?.WLog("couldnt find output node, flow completed: " + outputNode?.Output);
                        SetStatus(FileStatus.Processed);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                nodeParameters.Result = NodeResult.Failure;
                nodeParameters.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                Logger.Instance?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                SetStatus(FileStatus.ProcessingFailed);
                return;
            }
        }
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
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, true);
            File.WriteAllBytes(file, data);
            nodeParameters.Logger?.ILog($"Time taken to download plugin '{plugin.PackageName}': " + (DateTime.Now.Subtract(dtDownload)));

            DateTime dtExtract = DateTime.Now;
            System.IO.Compression.ZipFile.ExtractToDirectory(file, destDir);
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

                            nodeParameters.Logger?.ILog("Trying to move file: " + dll.FullName + " to " + destDir);
                            dll.MoveTo(Path.Combine(destDir, dll.Name));
                            nodeParameters.Logger?.ILog("Moved file: " + dll.FullName + " to " + destDir);
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
        return (Node)node;

    }
}
