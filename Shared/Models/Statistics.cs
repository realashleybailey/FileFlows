namespace FileFlows.Shared.Models;

public class Statistics : FileFlowObject
{
    private Dictionary<string, ExecutedNodeStatistic> _ExecutedNodes = new ();
    public Dictionary<string, ExecutedNodeStatistic> ExecutedNodes
    {
        get => _ExecutedNodes;
        set
        {
            _ExecutedNodes = value ?? new Dictionary<string, ExecutedNodeStatistic>();
        }
    }

    public void RecordNode(ExecutedNode node)
    {
        if (string.IsNullOrEmpty(node?.NodeUid))
            return;

        if (this.ExecutedNodes.ContainsKey(node.NodeUid) == false)
        {
            ExecutedNodes.Add(node.NodeUid, new ExecutedNodeStatistic
            {
                Uid = node.NodeUid
            });
        }
        ExecutedNodes[node.NodeUid].Outputs.Add(new ExecutedNodeOutput
        {
            Output = node.Output,
            Duration = node.ProcessingTime
        });
    }
}

public class ExecutedNodeStatistic
{
    public string Uid { get; set; }

    List<ExecutedNodeOutput> _Outputs = new ();
    public List<ExecutedNodeOutput> Outputs
    {
        get => _Outputs;
        set
        {
            _Outputs = value ?? new ();
        }
    }
}

public class ExecutedNodeOutput
{
    public int Output { get; set; }
    public TimeSpan Duration { get; set; }
}