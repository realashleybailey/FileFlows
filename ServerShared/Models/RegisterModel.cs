namespace FileFlows.ServerShared.Models
{
    public class RegisterModel
    {
        public string Address { get; set; }
        public string TempPath { get; set; }
        public int FlowRunners { get; set; }
        public bool Enabled { get; set; }
        public List<RegisterModelMapping> Mappings { get; set; }
    }

    public class RegisterModelMapping
    {
        public string Server { get; set; }
        public string Local { get; set; }
    }
}
