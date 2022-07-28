using System.Text.RegularExpressions;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service used to interact with the repository
/// </summary>
class RepositoryService
{
    private FileFlowsRepository repo;
    const string BASE_URL = "https://raw.githubusercontent.com/revenz/FileFlowsRepository/master/";


    public async Task Init()
    {
        repo = await GetRepository();
    }

    /// <summary>
    /// Get the repository data which contains information on all the scripts
    /// </summary>
    /// <returns>the repository data which contains information on all the scripts</returns>
    /// <exception cref="Exception">If failed to load the repository</exception>
    internal async Task<FileFlowsRepository> GetRepository()
    {
        string url = BASE_URL + "repo.json?ts=" + DateTime.UtcNow.ToFileTimeUtc();
        var srResult = await HttpHelper.Get<FileFlowsRepository>(url);
        if (srResult.Success == false)
            throw new Exception(srResult.Body);
        return srResult.Data;
    }

    internal Task DownloadSharedScripts() =>
        DownloadObjects(repo.SharedScripts, DirectoryHelper.ScriptsDirectoryShared);
    
    /// <summary>
    /// Downloads the function scripts from the repository
    /// </summary>
    internal Task DownloadFunctionScripts()
        => DownloadObjects(repo.FunctionScripts, DirectoryHelper.ScriptsDirectoryFunction);
    
    /// <summary>
    /// Downloads the flow templates from the repository
    /// </summary>
    internal Task DownloadFlowTemplates()
        => DownloadObjects(repo.FlowTemplates, DirectoryHelper.TemplateDirectoryFlow);
    
    /// <summary>
    /// Downloads the library templates from the repository
    /// </summary>
    internal Task DownloadLibraryTemplates()
        => DownloadObjects(repo.LibraryTemplates, DirectoryHelper.TemplateDirectoryLibrary);
    

    private async Task DownloadObjects(IEnumerable<RepositoryObject> objects, string destination)
    {
        foreach (var obj in objects)
        {
            string output = obj.Path;
            output = Regex.Replace(output, @"^Scripts\/[^\/]+\/", string.Empty);
            output = Regex.Replace(output, @"^Templates\/[^\/]+\/", string.Empty);
            await DownloadObject(obj.Path, Path.Combine(destination, output));
        }
    }

    /// <summary>
    /// Downlaods an object and saves it to disk
    /// </summary>
    /// <param name="path">the path identifier in the repository</param>
    /// <param name="outputFile">the filename where to save the file</param>
    private async Task DownloadObject(string path, string outputFile)
    {
        try
        {
            string content = await GetContent(path);
            content = "// path: " + path + "\n\n" + content;
            var dir = new FileInfo(outputFile).Directory;
            if(dir.Exists == false)
                dir.Create();
            await System.IO.File.WriteAllTextAsync(outputFile, content);
        }
        catch (Exception ex)
        { 
            Logger.Instance?.ELog($"Failed downloading script: '{path}' => {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the content of a repository object
    /// </summary>
    /// <param name="path">the repository object path</param>
    /// <returns>the repository object content</returns>
    public async Task<string> GetContent(string path)
    {
        string url = BASE_URL + path;
        var result = await HttpHelper.Get<string>(url);
        if (result.Success == false)
            throw new Exception(result.Body);
        return result.Data;
    }

    /// <summary>
    /// Update all the repository objects
    /// </summary>
    internal async Task Update()
    {
        await UpdateScripts();
        await UpdateTemplates();
    }

    /// <summary>
    /// Updates all the downloaded scripts from the repo
    /// </summary>
    internal async Task UpdateScripts()
    {
        var files = Directory.GetFiles(DirectoryHelper.ScriptsDirectory, "*.js", SearchOption.AllDirectories);
        List<string> knownPaths = repo.FlowScripts.Union(repo.FunctionScripts).Union(repo.SharedScripts)
            .Union(repo.SystemScripts).Select(x => x.Path).ToList();
        await UpdateObjects(files, knownPaths);
    }
    
    
    /// <summary>
    /// Updates all the downloaded templates from the repo
    /// </summary>
    internal async Task UpdateTemplates()
    {
        var files = Directory.GetFiles(DirectoryHelper.TemplateDirectory, "*.json", SearchOption.AllDirectories);
        List<string> knownPaths = repo.LibraryTemplates.Union(repo.FlowTemplates).Select(x => x.Path).ToList();
        await UpdateObjects(files, knownPaths);
    }

    private async Task UpdateObjects(IEnumerable<string> files, List<string> knownPaths)
    {
        List<Task> tasks = new();
        foreach (string file in files)
        {
            try
            {
                string line = (await System.IO.File.ReadAllLinesAsync(file)).First();
                if (line?.StartsWith("// path:") == false)
                    continue;
                string path = line.Substring("// path:".Length).Trim();
                if(knownPaths.Contains(path))
                    tasks.Add(DownloadObject(path, file));
            }
            catch (Exception)
            {
            }
        }

        Task.WaitAll(tasks.ToArray());
        
    }

    /// <summary>
    /// Downloads objects from the repository
    /// </summary>
    /// <param name="paths">paths of the objects to download</param>
    internal async Task DownloadObjects(List<string> paths)
    {
        foreach (string path in paths)
        {
            await DownloadObject(path, Path.Combine(DirectoryHelper.ScriptsDirectory, "..", path));
        }
    }
}