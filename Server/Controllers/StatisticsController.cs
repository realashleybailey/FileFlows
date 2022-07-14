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
    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    [HttpPost("record")]
    public async Task Record([FromBody] Statistic statistic)
    {
        if (statistic == null)
            return;
        if (LicenseHelper.IsLicensed() == false)
            return; // only save this to an external database
        await DbHelper.RecordStatistic(statistic);
    }

    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    [HttpGet("by-name/{name}")]
    public Task<IEnumerable<Statistic>> GetStatisticsByName([FromRoute] string name)
    {
        if (LicenseHelper.IsLicensed() == false)
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