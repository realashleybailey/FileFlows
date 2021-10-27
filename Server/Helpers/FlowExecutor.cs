namespace ViWatcher.Server.Helpers
{
    using ViWatcher.Shared.Models;
    using ViWatcher.Shared.Nodes;

    public class FlowExecutor 
    {
        public Flow Flow { get; set; }

        public Task<Shared.Models.NodeParameters> Run(string input)
        {
            return Task.Run(() =>
            {

                Shared.Models.NodeParameters args = new Shared.Models.NodeParameters();
                args.FileName = input;
                args.Result = Shared.NodeResult.Success;
                args.Logger = new Shared.FlowLogger();
                bool flowCompleted = false;
                int count = 0;

                // find the first node
                var part = Flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
                if (part == null)
                    return args; // no node to execute

                while (flowCompleted == false && count < 20)
                {
                    try
                    {
                        args.Logger.DLog("Executing part:" + part.Name);
                        var node = NodeHelper.LoadNode(part);
                        args.Logger.DLog("node: " + node);
                        int output = node.Execute(args);
                        args.Logger.DLog("output: " + output);
                        if (output == -1)
                        {
                            args.Logger.DLog("flow completed");
                            // flow has completed
                            flowCompleted = true;
                            break;
                        }
                        // we need the connection details
                        // need recurision here, since a node could have multiple output connections from the same output
                        // and we need to clone the input parameters at this point, since we may alter them differently on each path
                        // for now we're just doing one
                        var outputNode = part.OutputConnections.Where(x => x.Output == output).FirstOrDefault();
                        if (outputNode == null)
                            args.Logger.WLog("output node not found: " + output);
                        part = outputNode == null ? null : Flow.Parts.Where(x => x.Uid == outputNode.InputNode).FirstOrDefault();
                        if (part == null)
                        {
                            // couldnt find the connection, maybe bad data, but flow has now finished
                            args.Logger.WLog("couldnt find output node, flow completed: " + outputNode?.Output);
                            flowCompleted = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        args.Result = Shared.NodeResult.Failure;
                        args.Logger.ELog("Execution error: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                }

                return args;
            });
        }
    }
}