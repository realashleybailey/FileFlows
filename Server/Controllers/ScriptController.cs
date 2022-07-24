using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Script controller
/// </summary>
[Route("/api/script")]
public class ScriptController : Controller
{
    private const string UnsafeCharacters = "<>:\"/\\|?*";
    
    /// <summary>
    /// Gets all scripts in the system
    /// </summary>
    /// <returns>a list of all scripts</returns>
    [HttpGet]
    public async Task<IEnumerable<Script>> GetAll()
    {
        List<Script> scripts = new();
        scripts.AddRange(await GetRepositoryScripts(ScriptType.Flow));
        scripts.AddRange(await GetUserScripts(ScriptType.Flow));
        scripts.AddRange(await GetRepositoryScripts(ScriptType.System));
        scripts.AddRange(await GetUserScripts(ScriptType.System));
        
        scripts = scripts.DistinctBy(x => x.Name).ToList();
        var dictScripts = scripts.ToDictionary(x => x.Name.ToLower(), x => x);
        var flows = await new FlowController().GetAll();
        string flowTypeName = typeof(Flow).FullName;
        foreach (var flow in flows)
        {
            if (flow?.Parts?.Any() != true)
                continue;
            foreach (var p in flow.Parts)
            {
                if (p.FlowElementUid.StartsWith("Script:") == false)
                    continue;
                string scriptName = p.FlowElementUid[7..].ToLower();
                if (dictScripts.ContainsKey(scriptName) == false)
                    continue;
                var script = dictScripts[scriptName];
                script.UsedBy ??= new();
                if (script.UsedBy.Any(x => x.Uid == flow.Uid))
                    continue;
                script.UsedBy.Add(new ()
                {
                    Name = flow.Name,
                    Type = flowTypeName,
                    Uid = flow.Uid
                });
            }
        }

        return scripts.OrderBy(x => x.Name);
    }

    private Task<IEnumerable<Script>> GetRepositoryScripts(ScriptType type) => GetAll(type == ScriptType.System ? DirectoryHelper.ScriptsDirectorySystemRepository : DirectoryHelper.ScriptsDirectoryFlowRepository, type, repository: true);
    private Task<IEnumerable<Script>> GetUserScripts(ScriptType type) => GetAll(type == ScriptType.System ? DirectoryHelper.ScriptsDirectorySystemUser : DirectoryHelper.ScriptsDirectoryFlowUser, type);

    async Task<IEnumerable<Script>> GetAll(string directory, ScriptType type, bool repository = false)
    {
        List<Script> scripts = new();
        foreach (var file in new DirectoryInfo(directory).GetFiles("*.js"))
        {
            string name = file.Name.Replace(".js", "");
            scripts.Add(new()
            {
                Uid = name,
                Name = name,
                Repository = repository,
                Type = type,
                Code = await System.IO.File.ReadAllTextAsync(file.FullName)
            });
        }

        return scripts.OrderBy(x => x.Name);
    }

    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the script instance</returns>
    [HttpGet("{name}")]
    public async Task<Script> Get([FromRoute] string name, ScriptType type = ScriptType.Flow)
    {
        var result = FindScript(name, type);
        string code = await System.IO.File.ReadAllTextAsync(result.File);
        return new Script()
        {
            Uid = name,
            Name = name,
            Repository = result.System,
            Code = code
        };
    }

    private (bool System, string File) FindScript(string name, ScriptType type)
    {
        if (ValidScriptName(name) == false)
        {
            Logger.Instance.ELog("Script not found, name invalid: " + name);
            throw new Exception("Script not found");
        }

        string repoFilename = GetFullFilename(name, true, type);
        if (System.IO.File.Exists(repoFilename))
        {
            return (true, repoFilename);
        }

        string userFilename = GetFullFilename(name, false, type);
        if (System.IO.File.Exists(userFilename))
            return (false, userFilename);
        Logger.Instance.ELog($"Script '{name}' not found: {repoFilename}");
        Logger.Instance.ELog($"Script '{name}' not found: {userFilename}");
        throw new Exception("Script not found");

    }

    /// <summary>
    /// Gets the code for a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <param name="type">The type of script</param>
    /// <returns>the code for a script</returns>
    [HttpGet("{name}/code")]
    public async Task<string> GetCode(string name, [FromQuery] ScriptType type = ScriptType.Flow)
    {
        if (ValidScriptName(name) == false)
            return $"Logger.ELog('invalid name: {name.Replace("'", "''")}');\nreturn -1";
        try
        {
            var result = FindScript(name, type);
            return await System.IO.File.ReadAllTextAsync(result.File);
        }
        catch (Exception ex)
        {
            return $"Logger.ELog('Failed reading script: {ex.Message}');\nreturn -1";
        }
    }

    /// <summary>
    /// Saves a script
    /// </summary>
    /// <param name="script">The script to save</param>
    /// <returns>the saved script instance</returns>
    [HttpPost]
    public Script Save([FromBody] Script script)
    {
        if(ValidScriptName(script.Name) == false)
            throw new Exception("Invalid script name\nCannot contain: " + UnsafeCharacters);
        
        if (SaveScript(script.Name, script.Code, script.Type) == false)
            throw new Exception("Failed to save script");
        if (script.Uid != script.Name)
        {
            if (DeleteScript(script.Uid, false, script.Type))
            {
                UpdateScriptReferences(script.Uid, script.Name);
            }
            script.Uid = script.Name;
        }

        return script;
    }

    private async Task UpdateScriptReferences(string oldName, string newName)
    {
        var controller = new FlowController();
        var flows = await controller.GetAll();
        foreach (var flow in flows)
        {
            if (flow.Parts?.Any() != true)
                continue;
            bool changed = false;
            foreach (var part in flow.Parts)
            {
                if (part.FlowElementUid == "Script:" + oldName)
                {
                    part.FlowElementUid = "Script:" + newName;
                    changed = true;
                }
            }
            if(changed)
            {
                await controller.Update(flow);
            }
        }
    }

    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <param name="type">The type of scripts being deleted</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public void Delete([FromBody] ReferenceModel<string> model, [FromQuery] ScriptType type = ScriptType.Flow)
    {
        foreach (string m in model.Uids)
        {
            if (ValidScriptName(m) == false)
                continue;
            string file = GetFullFilename(m, false, type);
            if (System.IO.File.Exists(file) == false)
                file = GetFullFilename(m, true, type);
            if (System.IO.File.Exists(file) == false)
                continue;
            try
            {
                System.IO.File.Delete(file);
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Failed to delete script '{m}': {ex.Message}");
            }
        }
    }

    private bool DeleteScript(string script, bool repository, ScriptType type)
    {
        if (ValidScriptName(script) == false)
            return false;
        string file = GetFullFilename(script, repository, type);
        if (System.IO.File.Exists(file) == false)
            return false;
        try
        {
            System.IO.File.Delete(file);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed to delete script '{script}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Exports a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>A download response of the script</returns>
    [HttpGet("export/{name}")]
    public async Task<IActionResult> Export([FromRoute] string name)
    {
        var script = await GetCode(name);
        if (script == null)
            return NotFound();
        byte[] data = System.Text.UTF8Encoding.UTF8.GetBytes(script);
        return File(data, "application/octet-stream", name + ".js");
    }

    /// <summary>
    /// Imports a script
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="code">The code</param>
    [HttpPost("import")]
    public Script Import([FromQuery(Name = "filename")] string name, [FromBody] string code)
    {
        // will throw if any errors
        name = name.Replace(".js", "").Replace(".JS", "");
        name = GetNewUniqueName(name);
        return Save(new () { Name = name, Code = code, Repository = false});
    }

    /// <summary>
    /// Duplicates a script
    /// </summary>
    /// <param name="uid">The name of the script to duplicate</param>
    /// <returns>The duplicated script</returns>
    [HttpGet("duplicate/{name}")]
    public async Task<Script> Duplicate([FromRoute] string name)
    {
        // use DbHelper to avoid the cache, otherwise we would update the in memory object 
        var script = await Get(name);
        if (script == null)
            return null;
        
        script.Name = GetNewUniqueName(name);
        script.Repository = false;
        script.Uid = script.Name;
        return Save(script);
    }

    private string GetNewUniqueName(string name)
    {
        List<string> names = new DirectoryInfo(DirectoryHelper.ScriptsDirectory).GetFiles("*.js", SearchOption.AllDirectories).Select(x => x.Name.Replace(".js", "")).ToList();
        return UniqueNameHelper.GetUnique(name, names);
    }
    
    private bool ValidScriptName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (name.Contains(".."))
            return false;
        foreach (char c in UnsafeCharacters.Union(Enumerable.Range(0, 31).Select(x => (char)x)))
        {
            if (name.IndexOf(c) >= 0)
                return false;
        }
        return true;
    }

    private string GetFullFilename(string name, bool repository, ScriptType type)
    {
        string baseDir;
        if (repository && type == ScriptType.Flow)
            baseDir = DirectoryHelper.ScriptsDirectoryFlowRepository;
        else if (repository && type == ScriptType.System)
            baseDir = DirectoryHelper.ScriptsDirectorySystemRepository;
        else if (repository == false && type == ScriptType.System)
            baseDir = DirectoryHelper.ScriptsDirectorySystemUser;
        else
            baseDir = DirectoryHelper.ScriptsDirectoryFlowUser;
        
        return new FileInfo(Path.Combine(baseDir, name + ".js")).FullName;
    } 

    private bool SaveScript(string name, string code, ScriptType type)
    {
        try
        {
            new ScriptParser().Parse(name ?? string.Empty, code);
            
            if(ValidScriptName(name) == false)
                throw new Exception("Invalid script name:" + name);
            string file = GetFullFilename(name, false, type);
            System.IO.File.WriteAllText(file, code);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed saving script '{name}': {ex.Message}");
            return false;
        }
    }
}
