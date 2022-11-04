using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for NVIDIA
/// </summary>
[Route("api/nvidia")]
public class NvidiaController : Controller
{
    /// <summary>
    /// Gets the NVIDIA SMI data
    /// </summary>
    /// <returns>the NVIDIA SMI data</returns>
    [HttpGet("smi")]
    public object GetSmi()
    {
        var data = new NvidiaSmi().GetData();
        return data;
    }
}