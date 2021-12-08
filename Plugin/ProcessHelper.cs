namespace FileFlows.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    public struct ProcessResult
    {
        public bool Completed;
        public int? ExitCode;
        public string Output;
    }

    public class ExecuteArgs
    {
        public string Command { get; set; } 
        public string Arguments { get; set; }
        public int Timeout { get; set; }
        public string WorkingDirectory { get; set; }
        public delegate void OutputRecievedEvent(string output);
        public event OutputRecievedEvent StandardOutput;
        public event OutputRecievedEvent ErrorOutput;

        internal void OnStandardOutput(string output) => StandardOutput?.Invoke(output);
        internal void OnErrorOutput(string output) => ErrorOutput?.Invoke(output);
    }

    public class ProcessHelper
    {
        private Process process;
        private ILogger Logger;
        private ExecuteArgs Args;

        StringBuilder outputBuilder, errorBuilder;
        TaskCompletionSource<bool> outputCloseEvent, errorCloseEvent;



        public ProcessHelper(ILogger logger)
        {
            this.Logger = logger;
        }

        public void Cancel()
        {
            try
            {
                if (this.process != null)
                {
                    this.process.Kill();
                    this.process = null;
                }

            }
            catch (Exception) { }
        }

        public async Task<ProcessResult> ExecuteShellCommand(ExecuteArgs args)
        {
            var result = new ProcessResult();
            this.Args = args;

            using (var process = new Process())
            {
                this.process = process;
                // If you run bash-script on Linux it is possible that ExitCode can be 255.
                // To fix it you can try to add '#!/bin/bash' header to the script.

                process.StartInfo.FileName = args.Command;
                process.StartInfo.Arguments = args.Arguments ?? string.Empty;
                if(string.IsNullOrEmpty(args.WorkingDirectory) == false)
                    process.StartInfo.WorkingDirectory = args.WorkingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                Logger?.ILog(new string('=', 70));
                Logger?.ILog($"Executing: {args.Command} {args.Arguments}");
                if (string.IsNullOrEmpty(args.WorkingDirectory) == false)
                    Logger?.ILog($"Working Directory: {args.WorkingDirectory}");
                Logger?.ILog(new string('=', 70));

                outputBuilder = new StringBuilder();
                outputCloseEvent = new TaskCompletionSource<bool>();

                process.OutputDataReceived += OnOutputDataReceived;

                errorBuilder = new StringBuilder();
                errorCloseEvent = new TaskCompletionSource<bool>();

                process.ErrorDataReceived += OnErrorDataReceived;

                bool isStarted;

                try
                {
                    isStarted = process.Start();
                }
                catch (Exception error)
                {
                    // Usually it occurs when an executable file is not found or is not executable

                    result.Completed = true;
                    result.ExitCode = -1;
                    result.Output = error.Message;

                    isStarted = false;
                }

                if (isStarted)
                {
                    // Reads the output stream first and then waits because deadlocks are possible
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Creates task to wait for process exit using timeout
                    var waitForExit = WaitForExitAsync(process, args.Timeout);

                    // Create task to wait for process exit and closing all output streams
                    var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                    // Waits process completion and then checks it was not completed by timeout
                    if (
                        (
                            (args.Timeout > 0 && await Task.WhenAny(Task.Delay(args.Timeout), processTask) == processTask) ||
                            (args.Timeout == 0 && await Task.WhenAny(processTask) == processTask)
                        )
                         && waitForExit.Result)
                    {
                        result.Completed = true;
                        result.ExitCode = process.ExitCode;

                        // Adds process output if it was completed with error
                        if (process.ExitCode != 0)
                        {
                            result.Output = $"{outputBuilder}{errorBuilder}";
                        }
                    }
                    else
                    {
                        try
                        {
                            // Kill hung process
                            process.Kill();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            process = null;

            return result;
        }


        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // The output stream has been closed i.e. the process has terminated
            if (e.Data == null)
            {
                outputCloseEvent.SetResult(true);
            }
            else
            {
                Args?.OnStandardOutput(e.Data);
                Logger.ILog(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        }

        public void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            // The error stream has been closed i.e. the process has terminated
            if (e.Data == null)
            {
                errorCloseEvent.SetResult(true);
            }
            else
            {
                Args?.OnErrorOutput(e.Data);
                Logger.ILog(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        }


        private static Task<bool> WaitForExitAsync(Process process, int timeout)
        {
            if (timeout > 0)
                return Task.Run(() => process.WaitForExit(timeout));
            return Task.Run(() =>
            {
                process.WaitForExit();
                return Task.FromResult<bool>(true);
            });
        }
    }
}
