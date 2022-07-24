namespace FileFlows.Plugin.Models;

/// <summary>
/// Interface for executing a script
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// Executes a script
    /// </summary>
    /// <param name="args">the arguments of the script</param>
    /// <returns>the output node</returns>
    int Execute(ScriptExecutionArgs args);
}

/// <summary>
/// Arguments for script execution
/// </summary>
public class ScriptExecutionArgs
{
    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets the type of script being executed
    /// </summary>
    public ScriptType ScriptType { get; set; }

    /// <summary>
    /// Gets or sets teh NodeParameters
    /// </summary>
    public NodeParameters Args { get; set; }

    /// <summary>
    /// Gets a collection of additional arguments to be passed to the javascript executor
    /// </summary>
    public Dictionary<string, object> AdditionalArguments { get; set; }
}