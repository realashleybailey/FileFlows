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
    private CancellationToken CancellationToken = new CancellationToken();


    public FlowRunner(FlowExecutorInfo info, Flow flow, ProcessingNode node)
    {
        this.Info = info;
        this.Flow = flow;
        this.Node = node;
    }

    public delegate void FlowCompleted(FlowRunner sender, bool success);
    public event FlowCompleted OnFlowCompleted;

    public async Task Run()
    {
        try
        {
            var service = FlowRunnerService.Load();
            var updated = await service.Start(Info);
            if (updated == null)
                return; // failed to update
            Info.Uid = updated.Uid;
            RunActual();
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

    public async Task Finish()
    {
        var service = FlowRunnerService.Load();
        await service.Complete(Info);
        OnFlowCompleted?.Invoke(this, Info.LibraryFile.Status == FileStatus.Processed);
    }

    private void StepChanged(int step, string partName)
    {
        Info.CurrentPartName = partName;
        Info.CurrentPart = step;
        var service = FlowRunnerService.Load();
        service.Update(Info);
    }

    private void UpdatePartPercentage(float percentage)
    {
        Info.CurrentPartPercent = percentage;
        var service = FlowRunnerService.Load();
        service.Update(Info);
    }

    private void SetStatus(FileStatus status)
    {
        Info.LibraryFile.Status = status;
        var service = FlowRunnerService.Load();
        service.Update(Info);
    }

    private void RunActual() 
    {
        var args = new NodeParameters(Info.LibraryFile.Name, new FlowLogger()
        {
            LogFile = Path.Combine(Node.LoggingPath, Info.LibraryFile.Uid + ".log")
        });
        args.TempPath = Node.TempPath;
        args.RelativeFile = Info.LibraryFile.RelativePath;
        args.PartPercentageUpdate = UpdatePartPercentage;

        args.Logger!.ILog("Excecuting Flow: " + Flow.Name);

        //args.PartPercentageUpdate = (float percentage) => OnPartPercentageUpdate?.Invoke(percentage);

        var fiInput = new FileInfo(Info.LibraryFile.Name);
        args.Result = NodeResult.Success;
        args.GetToolPath = (string name) =>
        {
            // new Controllers.ToolController().GetByName(name)?.Result?.Path ?? "";
            var nodeService = NodeService.Load();
            return nodeService.GetToolPath(name).Result;
        };
        int count = 0;

        // find the first node
        var part = Flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
        if (part == null)
        {
            args.Logger!.ELog("Failed to find Input node");
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
                args.Logger?.WLog("Flow was canceled");
                args.Result = NodeResult.Failure;
                SetStatus(FileStatus.ProcessingFailed);                
                return;
            }
            if (part == null)
            {
                args.Logger?.WLog("Flow part was null");
                args.Result = NodeResult.Failure;
                SetStatus(FileStatus.ProcessingFailed);
                return;
            }

            try
            {

                args.Logger?.DLog("Executing part:" + (part!.Name?.EmptyAsNull() ?? part!.GetType().FullName ?? "unknown"));
                var currentNode = pluginLoader.LoadNode(part!).Result;

                if (currentNode == null)
                {
                    // happens when canceled    
                    args.Logger?.ELog("Failed to load node: " + part.Name);
                    SetStatus(FileStatus.ProcessingFailed);
                    args.Result = NodeResult.Failure;
                    return;
                }
                ++step;
                StepChanged(step, currentNode.Name);

                args.Logger?.DLog("node: " + currentNode.Name);
                int output = currentNode.Execute(args);

                args.Logger?.DLog("output: " + output);
                if (output == -1)
                {
                    // the execution failed                     
                    args.Logger?.ELog("node returned error code:", currentNode!.Name);
                    args.Result = NodeResult.Failure;
                    SetStatus(FileStatus.ProcessingFailed);
                    return;
                }
                var outputNode = part.OutputConnections?.Where(x => x.Output == output)?.FirstOrDefault();
                if (outputNode == null)
                {
                    args.Logger?.DLog("flow completed");
                    // flow has completed
                    args.Result = NodeResult.Success;
                    SetStatus(FileStatus.Processed);
                    return;
                }

                part = outputNode == null ? null : Flow.Parts.Where(x => x.Uid == outputNode.InputNode).FirstOrDefault();
                if (part == null)
                {
                    // couldnt find the connection, maybe bad data, but flow has now finished
                    args.Logger?.WLog("couldnt find output node, flow completed: " + outputNode?.Output);
                    SetStatus(FileStatus.Processed);
                    return;
                }
            }
            catch (Exception ex)
            {
                args.Result = NodeResult.Failure;
                args.Logger?.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                SetStatus(FileStatus.ProcessingFailed);
                return;
            }
        }
    }

}
