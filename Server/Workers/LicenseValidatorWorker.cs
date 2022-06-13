using FileFlows.Server.Helpers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker to validate and refresh the user license
/// </summary>
class LicenseValidatorWorker : Worker
{
    
    /// <summary>
    /// Creates a new instance of the license validator worker
    /// </summary>
    public LicenseValidatorWorker() : base(ScheduleType.Hourly, 3)
    {
    }

    /// <summary>
    /// Executes the worker
    /// </summary>
    protected override void Execute() => LicenseHelper.Update().Wait();
}