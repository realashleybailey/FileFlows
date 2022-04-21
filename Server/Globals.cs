namespace FileFlows.Server
{
    public class Globals
    {
        public static string Version = "0.5.2.734";
        public static bool IsDevelopment { get; set; }
        public static bool IsWindows { get; set; }

        public const string FileFlowsServer = "FileFlowsServer";


        public const string FlowFailName = "Fail Flow";
        private const string FailFlowUidStr = "fabbe59c-9d4d-4b6d-b1ef-4ed6585ac7cc";
        public static readonly Guid FailFlowUid = new Guid(FailFlowUidStr);
        public const string FailFlowDescription = "A system flow that will execute when another flow reports a failure, -1 from a node.";

        public const string FlowFailureInputUid = "FileFlows.BasicNodes.FlowFailure";
    }
}
 