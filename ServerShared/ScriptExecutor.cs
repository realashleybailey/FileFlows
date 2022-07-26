using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using FileFlows.ScriptExecution;
using FileFlows.Shared.Helpers;

namespace FileFlows.ServerShared;

/// <summary>
/// A Javascript code executor
/// </summary>
/// 
public class ScriptExecutor:IScriptExecutor
{
    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);

    /// <summary>
    /// Gets or sets the shared directory 
    /// </summary>
    public string SharedDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the URL to the FileFlows server 
    /// </summary>
    public string FileFlowsUrl { get; set; }
    
    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    public int Execute(ScriptExecutionArgs execArgs)
    {
        if (string.IsNullOrEmpty(execArgs?.Code))
            return -1; // no code, flow cannot continue doesnt know what to do

        var args = execArgs.Args;
        
        FileFlows.ScriptExecution.Executor executor = new();
        executor.Logger = new ScriptExecution.Logger();
        executor.Logger.ELogAction = (largs) => args.Logger.ELog(largs);
        executor.Logger.WLogAction = (largs) => args.Logger.WLog(largs);
        executor.Logger.ILogAction = (largs) => args.Logger.ILog(largs);
        executor.Logger.DLogAction = (largs) => args.Logger.DLog(largs);
        executor.HttpClient = HttpHelper.Client;
        if (string.IsNullOrWhiteSpace(FileFlowsUrl) == false)
        {
            if (args.Variables.ContainsKey("FileFlows.Url"))
                args.Variables["FileFlows.Url"] = FileFlowsUrl;
            else
                args.Variables.Add("FileFlows.Url", FileFlowsUrl);
        }

        executor.Variables = args.Variables;
        executor.SharedDirectory = SharedDirectory;

        
        executor.Code = execArgs.Code;
        if (execArgs.ScriptType == ScriptType.Flow && executor.Code.IndexOf("function Script") < 0)
        {
            executor.Code = "function Script() {\n" + executor.Code + "\n}\n";
            executor.Code += $"var scriptResult = Script();\nexport const result = scriptResult;";
        }
        
        executor.ProcessExecutor = new ScriptProcessExecutor(args);
        executor.AdditionalArguments.Add("Flow", args);
        executor.SharedDirectory = DirectoryHelper.ScriptsDirectoryShared;
        try
        {
            object result = executor.Execute();
            if (result is int iOutput)
                return iOutput;
            return -1;
        }
        catch (Exception ex)
        {
            args.Logger.ELog("Failed executing: " + ex.Message);
            return -1;
        }
    }

    /// <summary>
    /// Script Process Executor that executes the process using node parameters
    /// </summary>
    class ScriptProcessExecutor : IProcessExecutor
    {
        private NodeParameters NodeParameters;
        
        /// <summary>
        /// Constructs an instance of a Script Process Executor 
        /// </summary>
        /// <param name="nodeParameters">the node parameters</param>
        public ScriptProcessExecutor(NodeParameters nodeParameters)
        {
            this.NodeParameters = nodeParameters;
        }
        
        /// <summary>
        /// Executes the process
        /// </summary>
        /// <param name="args">the arguments to execute</param>
        /// <returns>the result of the execution</returns>
        public ProcessExecuteResult Execute(ProcessExecuteArgs args)
        {
            var result = NodeParameters.Execute(new ExecuteArgs()
            {
                Arguments = args.Arguments,
                Command = args.Command,
                Timeout = args.Timeout,
                ArgumentList = args.ArgumentList,
                WorkingDirectory = args.WorkingDirectory
            });
            return new ProcessExecuteResult()
            {
                Completed = result.Completed,
                Output = result.Output,
                ExitCode = result.ExitCode,
                StandardError = result.StandardError,
                StandardOutput = result.StandardOutput,
            };
        }
    }
}