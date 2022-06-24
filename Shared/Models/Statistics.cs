// namespace FileFlows.Shared.Models;
//
// /// <summary>
// /// Statistics of overall processed files
// /// </summary>
// public class Statistics : FileFlowObject
// {
//     private Dictionary<string, ExecutedNodeStatistic> _ExecutedNodes = new ();
//     
//     /// <summary>
//     /// Gets or set the nodes that have been executed
//     /// </summary>
//     public Dictionary<string, ExecutedNodeStatistic> ExecutedNodes
//     {
//         get => _ExecutedNodes;
//         set => _ExecutedNodes = value ?? new Dictionary<string, ExecutedNodeStatistic>();
//     }
//
//     /// <summary>
//     /// Records a node execution
//     /// </summary>
//     /// <param name="node">The node to record</param>
//     public void RecordNode(ExecutedNode node)
//     {
//         if (string.IsNullOrEmpty(node?.NodeUid))
//             return;
//
//         if (this.ExecutedNodes.ContainsKey(node.NodeUid) == false)
//         {
//             ExecutedNodes.Add(node.NodeUid, new ExecutedNodeStatistic
//             {
//                 Uid = node.NodeUid
//             });
//         }
//         ExecutedNodes[node.NodeUid].Outputs.Add(new ExecutedNodeOutput
//         {
//             Output = node.Output,
//             Duration = node.ProcessingTime
//         });
//     }
// }
//
//
// /// <summary>
// /// Statistics for executed nodes
// /// </summary>
// public class ExecutedNodeStatistic
// {
//     /// <summary>
//     /// Gets or sets the UID of the node 
//     /// </summary>
//     public string Uid { get; set; }
//
//     List<ExecutedNodeOutput> _Outputs = new ();
//     
//     /// <summary>
//     /// Gets or sets the recorded outputs of this node
//     /// </summary>
//     public List<ExecutedNodeOutput> Outputs
//     {
//         get => _Outputs;
//         set
//         {
//             _Outputs = value ?? new ();
//         }
//     }
// }
//
// /// <summary>
// /// Information about an executed node
// /// </summary>
// public class ExecutedNodeOutput
// {
//     /// <summary>
//     /// Gets or set the output of the executed node
//     /// </summary>
//     public int Output { get; set; }
//     
//     /// <summary>
//     /// Gets or sets the processing time of the executed node
//     /// </summary>
//     public TimeSpan Duration { get; set; }
// }