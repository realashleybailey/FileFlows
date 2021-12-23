namespace FileFlows.Node.FlowExecution;

using FileFlows.Plugin;
using FileFlows.ServerShared.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;


public class FlowRunner
{
    private FlowExecutorInfo Info;
    private Flow Flow;
    private ProcessingNode Node;
    private CancellationTokenSource CancellationToken = new CancellationTokenSource();

    public FlowRunner(FlowExecutorInfo info, Flow flow, ProcessingNode node)
    {
        this.Info = info;
        this.Flow = flow;
        this.Node = node;
    }

    public delegate void FlowCompleted(FlowRunner sender, bool success);
    public event FlowCompleted OnFlowCompleted;
    private NodeParameters nodeParameters;
    private Node currentNode;

    public async Task Run()
    {
        try
        {
            var service = FlowRunnerService.Load();
            var updated = await service.Start(Info);
            if (updated == null)
                return; // failed to update
            Info.Uid = updated.Uid;
            var communicator = FlowRunnerCommunicator.Load(Info.LibraryFile.Uid);
            communicator.OnCancel += Communicator_OnCancel;
            try
            {
                RunActual(communicator);
            }
            finally
            {
                communicator.OnCancel -= Communicator_OnCancel;
                communicator.Close();
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error in FlowRunner: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        finally
        {
            await Finish();
        }
    }

    private void Communicator_OnCancel()
    {
        CancellationToken.Cancel();
        if (currentNode != null)
            currentNode.Cancel().Wait();
    }

    public async Task Finish()
    {

        if (nodeParameters.Logger is FlowLogger fl)
            Info.Log = fl.ToString();
        await Complete();
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    private async Task Complete()
    {
        DateTime start = DateTime.Now;
        do
        {
            try
            {
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
        Info.CurrentPartPercent = percentage;
        if (diff < 0.1)
            return; // so small no need to tell server about update;
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
            Info.LibraryFile.FinalSize = nodeParameters.IsDirectory ? nodeParameters.GetDirectorySize(nodeParameters.WorkingFile) : nodeParameters.WorkingFileSize;
            Info.LibraryFile.OutputPath = Node.UnMap(nodeParameters.WorkingFile);
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
        Info.LibraryFile.OriginalSize = nodeParameters.IsDirectory ? nodeParameters.GetDirectorySize(nodeParameters.WorkingFile) : new FileInfo(nodeParameters.WorkingFile).Length;
        nodeParameters.TempPath = Node.TempPath;
        nodeParameters.RelativeFile = Info.LibraryFile.RelativePath;
        nodeParameters.PartPercentageUpdate = UpdatePartPercentage;

        nodeParameters.Logger!.ILog("Excecuting Flow: " + Flow.Name);

        nodeParameters.Result = NodeResult.Success;
        nodeParameters.GetToolPath = (string name) =>
        {
            var nodeService = NodeService.Load();
            return Node.Map(nodeService.GetToolPath(name).Result);
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
            if (CancellationToken.IsCancellationRequested)
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
                currentNode = pluginLoader.LoadNode(part!).Result;
                
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
                int output = currentNode.Execute(nodeParameters);

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
            catch (Exception ex)
            {
                nodeParameters.Result = NodeResult.Failure;
                nodeParameters.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                SetStatus(FileStatus.ProcessingFailed);
                return;
            }
        }
    }

}
