namespace FileFlows.Server.Controllers;

using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;

/// <summary>
/// Status controller
/// </summary>
[Route("/api/statistics")]
public class StatisticsController : Controller
{
    private static Statistics Instance;
    private static Mutex _mutex = new Mutex();

    /// <summary>
    /// Get the system settings
    /// </summary>
    /// <returns>The system settings</returns>
    [HttpGet]
    public async Task<Statistics> Get()
    {
        if(Request != null)
        {
            // coming from the web, check if telemetry is turned on, if not return no data
            bool telemetry = new SettingsController().Get()?.Result?.DisableTelemetry != true;
            if (telemetry == false)
                return new Statistics();
        }

        if (Instance != null)
            return Instance;
        _mutex.WaitOne();
        try
        {
            if (Instance == null)
            {
                Instance = await DbManager.Single<Statistics>();
                if (Instance.Uid == Guid.Empty)
                    await DbHelper.Update(Instance);
            }
            return Instance;
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }

    internal async Task RecordStatistics(LibraryFile file)
    {
        if (file == null)
            return;

        if (file.ExecutedNodes?.Any() != true)
            return;

        var stats = await Get();

        foreach (var node in file.ExecutedNodes)
        {
            stats.RecordNode(node);
        }

        await DbManager.Update(stats);
    }
}
