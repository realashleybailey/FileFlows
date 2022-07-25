namespace FileFlows.ScriptExecution;

/// <summary>
/// Interface used to execute a process from a script
/// </summary>
public interface IProcessExecutor
{
    ProcessExecuteResult Execute(ProcessExecuteArgs args);
}

/// <summary>
/// The result of a process execution
/// </summary>
public class ProcessExecuteResult
{
    /// <summary>
    /// If the processed completed, or if it was aborted
    /// </summary>
    public bool Completed { get; set; }

    /// <summary>
    /// The exit code of the process
    /// </summary>
    public int? ExitCode{ get; set; }
    
    /// <summary>
    /// The output of the process
    /// </summary>
    public string Output{ get; set; } = string.Empty;

    /// <summary>
    /// The standard output from the process
    /// </summary>
    public string StandardOutput{ get; set; } = string.Empty;

    /// <summary>
    /// The error output from the process
    /// </summary>
    public string StandardError { get; set; } = string.Empty;
}

/// <summary>
/// Arguments used to execute a process
/// </summary>
public class ProcessExecuteArgs
{
    /// <summary>
    /// Gets or sets the command to execute
    /// </summary>
    public string Command { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the arguments of the command
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments of the command as a list and will be correctly escaped
    /// </summary>
    public string[] ArgumentList { get; set; } = new string[] { };
    /// <summary>
    /// Gets or sets the timeout in seconds of the process
    /// </summary>
    public int Timeout { get; set; }
    /// <summary>
    /// Gets or sets the working directory of the process
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}