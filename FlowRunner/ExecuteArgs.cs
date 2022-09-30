using FileFlows.Shared.Models;

namespace FileFlows.FlowRunner;

class ExecuteArgs
{
    public bool IsServer { get; set; }
    public string TempDirectory { get; set; }
    public string ConfigDirectory { get; set; }
    public ConfigurationRevision Config { get; set; }
    public Guid LibraryFileUid { get; set; }
    public string WorkingDirectory { get; set; }
    public string Hostname { get; set; }
}