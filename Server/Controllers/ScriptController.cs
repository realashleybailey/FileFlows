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
        foreach (var file in new DirectoryInfo(DirectoryHelper.ScriptsDirectory).GetFiles("*.js"))
        {
            string name = file.Name.Replace(".js", "");
            scripts.Add(new()
            {
                Uid = name,
                Name = name,
                Code = await System.IO.File.ReadAllTextAsync(file.FullName)
            });
        }

        return scripts.OrderBy(x => x.Name);
    }


    /// <summary>
    /// Get a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>the script instance</returns>
    [HttpGet("{uid}")]
    public async Task<Script> Get(string name)
    {
        if (ValidScriptName(name) == false)
            throw new Exception("Script not found");
        var file = GetFullFilename(name);
        if (System.IO.File.Exists(file) == false)
            throw new Exception("Script not found");
        string code = await System.IO.File.ReadAllTextAsync(file);
        return new Script()
        {
            Uid = name,
            Name = name,
            Code = code
        };
    }

    /// <summary>
    /// Gets the code for a script
    /// </summary>
    /// <param name="name">The name of the script</param>
    /// <returns>the code for a script</returns>
    [HttpGet("{name}/code")]
    public async Task<string> GetCode(string name)
    {
        if (ValidScriptName(name) == false)
            return $"Logger.ELog('invalid name: {name.Replace("'", "''")}');\nreturn -1";
        try
        {
            string file = Path.Combine(DirectoryHelper.ScriptsDirectory, name + ".js");
            if (System.IO.File.Exists(file) == false)
                return "Logger.ELog('script not found');\nreturn -1";
            return await System.IO.File.ReadAllTextAsync(file);
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
        
        if (SaveScript(script.Name, script.Code) == false)
            throw new Exception("Failed to save script");
        if (script.Uid != script.Name)
        {
            Delete(new ReferenceModel<string>() { Uids = new[] { script.Uid } });
            script.Uid = script.Name;
        }

        return script;
    }

    /// <summary>
    /// Delete scripts from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public void Delete([FromBody] ReferenceModel<string> model)
    {
        foreach (string m in model.Uids)
        {
            if (ValidScriptName(m) == false)
                continue;
            string file = GetFullFilename(m);
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
        return Save(new () { Name = name, Code = code});
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
        script.Uid = script.Name;
        return Save(script);
    }

    private string GetNewUniqueName(string name)
    {
        List<string> names = new DirectoryInfo(DirectoryHelper.ScriptsDirectory).GetFiles("*.js").Select(x => x.Name.Replace(".js", "")).ToList();
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
    
    private string GetFullFilename(string name) => 
        new DirectoryInfo(Path.Combine(DirectoryHelper.ScriptsDirectory, name + ".js")).FullName;

    private bool SaveScript(string name, string code)
    {
        try
        {
            new ScriptParser().Parse(name ?? string.Empty, code);
            
            if(ValidScriptName(name) == false)
                throw new Exception("Invalid script name:" + name);
            string file = GetFullFilename(name);
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
