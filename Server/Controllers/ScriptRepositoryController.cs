using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for the Script Repository
/// </summary>
[Route("/api/script-repo")]
public class ScriptRepositoryController : Controller
{
    const string BASE_URL = "https://raw.githubusercontent.com/revenz/FileFlowsScripts/master/";
    
    /// <summary>
    /// Gets the scripts
    /// </summary>
    /// <param name="type">the type of scripts to get</param>
    /// <param name="missing">only include scripts not downloaded</param>
    /// <returns>a collection of scripts</returns>
    [HttpGet("scripts")]
    public async Task<IEnumerable<RepositoryScript>> GetScripts([FromQuery] ScriptType type, [FromQuery] bool missing = true)
    {
        var repo = await GetRepository();
        var scripts = (type == ScriptType.System ? repo.SystemScripts : repo.FlowScripts);
        return scripts;
    }

    private async Task<ScriptRepository> GetRepository()
    {
        string url = BASE_URL + "repo.json?ts=" + DateTime.UtcNow.ToFileTimeUtc();
        var srResult = await HttpHelper.Get<ScriptRepository>(url);
        if (srResult.Success == false)
            throw new Exception(srResult.Body);
        return srResult.Data;
    }

    /// <summary>
    /// Gets the code of a script
    /// </summary>
    /// <param name="path">the script path</param>
    /// <returns>the script code</returns>
    [HttpGet("code")]
    public async Task<string> GetCode([FromQuery] string path)
    {
        string url = BASE_URL + path;
        var result = await HttpHelper.Get<string>(url);
        if (result.Success == false)
            throw new Exception(result.Body);
        return result.Data;
    }
    
    /// <summary>
    /// Download plugins into the FileFlows system
    /// </summary>
    /// <param name="model">A list of plugins to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download")]
    public async Task Download([FromBody] DownloadModel model)
    {
        if (model == null || model.Scripts?.Any() != true)
            return; // nothing to delete

        // always re-download all the shared scripts to ensure they are up to date
        var repo = await GetRepository();
        foreach (var script in repo.SharedScripts)
        {
            string output = script.Path;
            if (output.StartsWith("Shared/"))
                output = output[7..];
            await DownloadScript(script.Path, Path.Combine(DirectoryHelper.ScriptsDirectoryShared, output));
        }

        foreach(var script in model.Scripts)
        {
            try
            {
                int slashIndex = script.IndexOf("/");
                var type = Enum.Parse<ScriptType>(script[0..slashIndex]);
                string file = script[(slashIndex + 1)..].Replace("/", " - ");
                if (file.EndsWith(".js") == false)
                    file += ".js";
                string output =
                    Path.Combine(
                        type == ScriptType.System
                            ? DirectoryHelper.ScriptsDirectorySystemRepository
                            : DirectoryHelper.ScriptsDirectoryFlowRepository, file);
                await DownloadScript(script, output);
            }
            catch (Exception ex)
            { 
                Logger.Instance?.ELog($"Failed downloading script: '{script}' => {ex.Message}");
            }
        }
    }

    private async Task DownloadScript(string path, string output)
    {
        try
        {
            string code = await GetCode(path);
            await System.IO.File.WriteAllTextAsync(output, code);
        }
        catch (Exception ex)
        { 
            Logger.Instance?.ELog($"Failed downloading script: '{path}' => {ex.Message}");
        }
    }
    
    /// <summary>
    /// Download model
    /// </summary>
    public class DownloadModel
    {
        /// <summary>
        /// A list of plugin packages to download
        /// </summary>
        public List<string> Scripts { get; set; }
    }
}