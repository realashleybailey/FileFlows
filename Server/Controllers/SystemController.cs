using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// System Controller
/// </summary>
[Route("/api/system")]
public class SystemController:Controller
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    [HttpGet("version")]
    public string Version() => Node.Globals.Version;

    /// <summary>
    /// Gets the node updater
    /// </summary>
    /// <returns>the node updater</returns>
    [HttpGet("node-updater")]
    public object GetNodeUpdater()
    {
        return null;
    }
}