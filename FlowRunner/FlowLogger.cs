namespace FileFlows.FlowRunner;

using System;
using System.Linq;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

public class FlowLogger : ILogger
{
    List<string> log = new List<string>();
    public void DLog(params object[] args) => Log(LogType.Debug, args);
    public void ELog(params object[] args) => Log(LogType.Error, args);
    public void ILog(params object[] args) => Log(LogType.Info, args);
    public void WLog(params object[] args) => Log(LogType.Warning, args);

    public LibraryFile File { get; set; }
    private enum LogType
    {
        Error, Warning, Info, Debug
    }
    IFlowRunnerCommunicator Communicator;
    public FlowLogger(IFlowRunnerCommunicator communicator)
    {
        this.Communicator = communicator;
    }

    private void Log(LogType type, params object[] args)
    {
        if (args == null || args.Length == 0)
            return;
        string prefix = type switch
        {
            LogType.Info => "INFO",
            LogType.Error => "ERRR",
            LogType.Warning => "WARN",
            LogType.Debug => "DBUG",
            _ => ""
        };

        string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff") + " - " + prefix + " -> " +
            string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive || x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        log.Add(message);
        if(type != LogType.Debug)
            Console.WriteLine(message);
        try
        {
            Communicator.LogMessage(Program.Uid, message).Wait();
        }
        catch (Exception) { }
    }

    public override string ToString() => String.Join(Environment.NewLine, log);

    public string GetTail(int length = 50)
    {
        if (length <= 0)
            length = 50;

        var noLines = log.Where(x => x.Contains("======================================================================") == false);
        if (noLines.Count() <= length)
            return String.Join(Environment.NewLine, noLines);
        return String.Join(Environment.NewLine, noLines.Skip(noLines.Count() - length));
    }
}
