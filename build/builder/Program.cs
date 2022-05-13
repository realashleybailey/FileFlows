BuildOptions.Initialize(args);
// Clean up any previous files
DeployCleaner.Execute();

Logger.ILog("FileFlows Builder");

Globals.BuildNumber = Utils.GetBuildNumber();
Logger.ILog("Version: " + Globals.Version);

Logger.ILog("Windows: " + BuildOptions.IsWindows);
Logger.ILog("Linux: " + BuildOptions.IsLinux);
Logger.ILog("Docker: " + BuildOptions.IsDocker);
Logger.ILog("BuildAll: " + BuildOptions.BuildAll);

Logger.ILog("BuildOptions.SourcePath:" + BuildOptions.SourcePath);
var srcDir = new DirectoryInfo(BuildOptions.SourcePath);
foreach(var d in srcDir.GetDirectories())
    Logger.ILog("source sub dir: " + d.FullName);

if(SpellCheck.Execute() == false)
{
    Logger.ELog("Spelling mistakes detected, aborting build.");
    return;
}

File.WriteAllText(BuildOptions.Output + "/build-version-variable.ps1", 
$"Write-Output \"Setting env.FF_VERSION to: {Globals.MajorVersion}\"" + Environment.NewLine +
$"Write-Host \"##teamcity[setParameter name='env.FF_VERSION' value='{Globals.MajorVersion}']\"");

//new Server().MakeInstaller();
//return;

var components = typeof(Component).Assembly.GetTypes().Where(x => x.IsAbstract == false && x.IsSubclassOf(typeof(Component)));
foreach(var component in components)
{    
    if(BuildOptions.BuildComponent(component.Name))
        Component.Build(component);
}

