using System.Text;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;
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
        
        SystemEvents.OnLibraryFileAdd += SystemEventsOnOnLibraryFileAdd;
        SystemEvents.OnLibraryFileProcessed += SystemEventsOnOnLibraryFileProcessed;
        SystemEvents.OnLibraryFileProcessedFailed += SystemEventsOnOnLibraryFileProcessedFailed;
        SystemEvents.OnLibraryFileProcessedSuceess += SystemEventsOnOnLibraryFileProcessedSuceess;
        SystemEvents.OnLibraryFileProcessingStarted += SystemEventsOnOnLibraryFileProcessingStarted;
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

    private async Task RunTask(FileFlowsTask task, Dictionary<string, object> additionalVariables = null)
    {
        string code = await new ScriptController().GetCode(task.Script, type: ScriptType.System);
        if (string.IsNullOrWhiteSpace(code))
        {
            Logger.Instance.WLog($"No code found for Task '{task.Name}' using script: {task.Script}");
            return;
        }
        Logger.Instance.ILog("Executing task: " + task.Name);
        DateTime dtStart = DateTime.Now;

        var variables = Variables.ToDictionary(x => x.Key, x => x.Value);
        if (additionalVariables?.Any() == true)
        {
            foreach (var variable in additionalVariables)
            {
                if (variables.ContainsKey(variable.Key))
                    variables[variable.Key] = variable.Value;
                else
                    variables.Add(variable.Key, variable.Value);
            }
        }

        var result = ScriptExecutor.Execute(code, variables);
        if(result.Success)
            Logger.Instance.ILog($"Task '{task.Name}' completed in: " + (DateTime.Now.Subtract(dtStart)) + "\n" + result.Log);
        else
            Logger.Instance.ELog($"Error executing task '{task.Name}: " + result.ReturnValue + "\n" + result.Log);
    }

    private void LibraryFileEventTriggered(TaskType type, SystemEvents.LibraryFileEventArgs args)
    {
        var tasks = this.Tasks.Where(x => x.Type == type).ToArray();
        foreach (var task in tasks)
        {
            _ = RunTask(task, new Dictionary<string, object>
            {
                { "FileName", args.File.Name },
                { "LibraryFile", args.File },
                { "Library", args.Library }
            });
        }
    }

    private void SystemEventsOnOnLibraryFileAdd(SystemEvents.LibraryFileEventArgs args) =>
        LibraryFileEventTriggered(TaskType.FileAdded, args);
    private void SystemEventsOnOnLibraryFileProcessingStarted(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessing, args);
    private void SystemEventsOnOnLibraryFileProcessed(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessed, args);
    private void SystemEventsOnOnLibraryFileProcessedSuceess(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessSuccess, args);
    private void SystemEventsOnOnLibraryFileProcessedFailed(SystemEvents.LibraryFileEventArgs args)
        => LibraryFileEventTriggered(TaskType.FileProcessFailed, args);

}