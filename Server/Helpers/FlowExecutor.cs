namespace FileFlows.Server.Helpers
{
    using FileFlows.Shared.Models;
    using FileFlows.Plugin;
    using FileFlows.Server.Workers;

    public class FlowExecutor
    {
        public Flow Flow { get; set; }

        public ILogger Logger { get; set; }

        public delegate void PartPercentageUpdate(float percentage);
        public event PartPercentageUpdate OnPartPercentageUpdate;

        public delegate void StepChange(int currentStep, string stepName, string workingFile);

        public event StepChange OnStepChange;

        public Node? CurrentNode;

        public Task<NodeParameters> Run(string input, string relativePath, string tempPath, string logFile)
        {
            return Task.Run(() =>
            {
                var args = new NodeParameters(input);
                args.TempPath = tempPath;
                args.RelativeFile = relativePath;

                args.PartPercentageUpdate = (float percentage) => OnPartPercentageUpdate?.Invoke(percentage);

                var fiInput = new System.IO.FileInfo(input);
                args.Result = NodeResult.Success;
                args.Logger = Logger ?? new FlowLogger()
                {
                    LogFile = logFile
                };
                args.GetToolPath = (string name) => new Controllers.ToolController().GetByName(name)?.Path ?? "";
                bool flowCompleted = false;
                int count = 0;

                // find the first node
                var part = Flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
                if (part == null)
                    return args; // no node to execute

                int step = 0;
                if (OnStepChange != null)
                    OnStepChange(step, part.Name, args.WorkingFile);

                while (flowCompleted == false && count < 20)
                {
                    try
                    {
                        using var pluginLoader = new PluginHelper();

                        try
                        {
                            args.Logger.DLog("Executing part:" + part.Name);
                            CurrentNode = pluginLoader.LoadNode(part);
                            //CurrentNode = NodeHelper.LoadNode(context, part);

                            ++step;
                            if (OnStepChange != null)
                                OnStepChange(step, CurrentNode.Name, args.WorkingFile);

                            args.Logger.DLog("node: " + CurrentNode.Name);
                            int output = CurrentNode.Execute(args);
                            if (CurrentNode == null)
                            {
                                // happens when canceled    
                                args.Logger.ELog("node was canceled error code:", CurrentNode.Name);
                                args.Result = NodeResult.Failure;
                                flowCompleted = true;
                                break;
                            }
                            args.Logger.DLog("output: " + output);
                            if (output == -1)
                            {
                                // the execution failed                     
                                args.Logger.ELog("node returned error code:", CurrentNode.Name);
                                args.Result = NodeResult.Failure;
                                flowCompleted = true;
                                break;
                            }
                            var outputNode = part.OutputConnections?.Where(x => x.Output == output)?.FirstOrDefault();
                            if (outputNode == null)
                            {
                                args.Logger.DLog("flow completed");
                                // flow has completed
                                flowCompleted = true;
                                args.Result = NodeResult.Success;
                                break;
                            }
                            // we need the connection details
                            // need recurision here, since a node could have multiple output connections from the same output
                            // and we need to clone the input parameters at this point, since we may alter them differently on each path
                            // for now we're just doing one
                            part = outputNode == null ? null : Flow.Parts.Where(x => x.Uid == outputNode.InputNode).FirstOrDefault();
                            if (part == null)
                            {
                                // couldnt find the connection, maybe bad data, but flow has now finished
                                args.Logger.WLog("couldnt find output node, flow completed: " + outputNode?.Output);
                                flowCompleted = true;
                                break;
                            }
                        }
                        finally
                        {
                            CurrentNode = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        args.Result = NodeResult.Failure;
                        args.Logger.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }

                return args;
            });
        }

        public async Task Cancel()
        {
            if (CurrentNode != null)
            {
                var cn = CurrentNode;
                CurrentNode = null;
                await cn.Cancel();
            }
        }
    }
}