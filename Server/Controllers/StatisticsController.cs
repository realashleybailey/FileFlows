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
    private static SemaphoreSlim semaphore = new (1);

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
        await semaphore.WaitAsync();
        try
        {
            if (Instance == null)
            {
                Instance = await DbHelper.Single<Statistics>();
                if (Instance.Uid == Guid.Empty)
                    await DbHelper.Update(Instance);
            }
            return Instance;
        }
        finally
        {
            semaphore.Release();
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

        await DbHelper.Update(stats);
    }

    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    public async Task Record([FromBody] Statistic statistic)
    {
        if (DbHelper.UseMemoryCache)
            return; // only save this to an external database
        await DbHelper.RecordStatistc(statistic);
    }

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    [HttpGet("by-name/{name}")]
    public Task<IEnumerable<Statistic>> GetStatisticsByName([FromRoute] string name)
    {
        if (DbHelper.UseMemoryCache)
            throw new Exception("Not supported by this installation.");
        return DbHelper.GetStatisticsByName(name);
    }
}


/// <summary>
/// A statistic
/// </summary>
public class Statistic
{
    /// <summary>
    /// Gets or sets the name of the statistic
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value
    /// </summary>
    public object Value { get; set; }
}