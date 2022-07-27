using System.Text;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that runs FileFlows Tasks
/// </summary>
public class FileFlowsTasksWorker: Worker
{
    private static FileFlowsTasksWorker Instance;
    private readonly List<FileFlowsTask> Tasks = new ();
    private readonly Dictionary<string, object> Variables = new ();
    /// <summary>
    /// A list of tasks and the quarter they last ran in
    /// </summary>
    private Dictionary<Guid, int> TaskLastRun = new ();
    
    
    /// <summary>
    /// Creates a new instance of the Scheduled Task Worker
    /// </summary>
    public FileFlowsTasksWorker() : base(ScheduleType.Minute, 1)
    {
        Instance = this;
        ReloadTasks();
        ReloadVariables();
    }
    
    /// <summary>
    /// Reloads the stored list of tasks
    /// </summary>
    public static void ReloadTasks()
    {
        var list = new TaskController().GetAll().Result?.ToList();
        Instance.Tasks.Clear();
        if(list?.Any() == true)
            Instance.Tasks.AddRange(list);
    }
    
    /// <summary>
    /// Reloads the stored list of variables
    /// </summary>
    public static void ReloadVariables()
    {
        var list = new VariableController().GetAll().Result?.ToList();
        Instance.Variables.Clear();
        foreach (var var in list ?? new ())
        {
            Instance.Variables.Add(var.Name, var.Value);
        }
        
        if(Instance.Variables.ContainsKey("FileFlows.Url") == false)
            Instance.Variables.Add("FileFlows.Url", ServerShared.Services.Service.ServiceBaseUrl);
    }

    /// <summary>
    /// Executes any tasks
    /// </summary>
    protected override void Execute()
    {
        if (LicenseHelper.IsLicensed() == false)
            return;
        
        int quarter = TimeHelper.GetCurrentQuarter();
        // 0, 1, 2, 3, 4
        foreach (var task in this.Tasks)
        {
            if (task.Type != TaskType.Time)
                continue;
            if (task.Schedule[quarter] != '1')
                continue;
            if (TaskLastRun.ContainsKey(task.Uid) && TaskLastRun[task.Uid] == quarter)
                continue;
            _ = RunTask(task);
            if (TaskLastRun.ContainsKey(task.Uid))
                TaskLastRun[task.Uid] = quarter;
            else
                TaskLastRun.Add(task.Uid, quarter);
        }
    }

    private async Task RunTask(FileFlowsTask task)
    {
        string code = await new ScriptController().GetCode(task.Script, type: ScriptType.System);
        if (string.IsNullOrWhiteSpace(code))
        {
            Logger.Instance.WLog($"No code found for Task '{task.Name}' using script: {task.Script}");
            return;
        }
        Logger.Instance.ILog("Executing task: " + task.Name);
        DateTime dtStart = DateTime.Now;
        Executor executor = new Executor();
        executor.Code = code;
        executor.SharedDirectory = DirectoryHelper.ScriptsDirectoryShared;
        executor.HttpClient = HttpHelper.Client;
        executor.Logger = new ScriptExecution.Logger();
        StringBuilder sbLog = new();
        executor.Logger.DLogAction = (args) => StringBuilderLog(sbLog, LogType.Debug, args);
        executor.Logger.ILogAction = (args) => StringBuilderLog(sbLog, LogType.Info, args);
        executor.Logger.WLogAction = (args) => StringBuilderLog(sbLog, LogType.Warning, args);
        executor.Logger.ELogAction = (args) => StringBuilderLog(sbLog, LogType.Error, args);
        executor.Variables = Variables;
        try
        {
            executor.Execute();
            Logger.Instance.ILog($"Task '{task.Name}' completed in: " + (DateTime.Now.Subtract(dtStart)) + "\n" + sbLog);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Error executing task '{task.Name}: " + ex.Message + "\n" + sbLog);
        }
    }

    private void StringBuilderLog(StringBuilder builder, LogType type, params object[] args)
    {
        string typeString = type switch
        {
            LogType.Debug => "[DBUG] ",
            LogType.Info => "[INFO] ",
            LogType.Warning => "[WARN] ",
            LogType.Error => "[ERRR] ",
            _ => "",
        };
        string message = typeString + string.Join(", ", args.Select(x =>
            x == null ? "null" :
            x.GetType().IsPrimitive ? x.ToString() :
            x is string ? x.ToString() :
            System.Text.Json.JsonSerializer.Serialize(x)));
        builder.AppendLine(message);
    }
}