using System.Diagnostics;

namespace FileFlows.ScriptExecution;

/// <summary>
/// A basic process executor that will be used if no custom one is set
/// </summary>
class BasicProcessExecutor: IProcessExecutor
{
    private Logger Logger;
    public BasicProcessExecutor(Logger logger)
    {
        this.Logger = logger;
    }

    /// <summary>
    /// Executes the process
    /// </summary>
    /// <param name="args">the arguments to execute</param>
    /// <returns>the result of the execution</returns>
    public ProcessExecuteResult Execute(ProcessExecuteArgs args)
    {
        var result = new ProcessExecuteResult();

        using var process = new Process();
        try
        {
            process.StartInfo.FileName = args.Command;
            if (args.ArgumentList?.Any() == true)
            {
                args.Arguments = String.Empty;
                foreach (var arg in args.ArgumentList)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                    if (arg.IndexOf(" ") > 0)
                        args.Arguments += "\"" + arg + "\" ";
                    else
                        args.Arguments += arg + " ";
                }

                args.Arguments = args.Arguments.Trim();
            }
            else if (string.IsNullOrEmpty(args.Arguments) == false)
            {
                process.StartInfo.Arguments = args.Arguments;
            }

            if (string.IsNullOrEmpty(args.WorkingDirectory) == false)
                process.StartInfo.WorkingDirectory = args.WorkingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            Logger.ILog(new string('-', 70));
            Logger.ILog($"Executing: {args.Command} {args.Arguments}");
            if (string.IsNullOrEmpty(args.WorkingDirectory) == false)
                Logger.ILog($"Working Directory: {args.WorkingDirectory}");
            Logger.ILog(new string('-', 70));

            process.Start();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd().Trim();
            string errorOutput = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();
            
            result.Output = output;
            result.StandardOutput = output;
            result.StandardError = errorOutput;
            result.ExitCode = process.ExitCode;
            result.Completed = true;
        }
        catch (Exception ex)
        {
            result.Output = ex.Message;
            result.Completed = false;
        }

        return result;
    }
}