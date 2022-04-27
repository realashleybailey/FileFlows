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


var components = typeof(Component).Assembly.GetTypes().Where(x => x.IsAbstract == false && x.IsSubclassOf(typeof(Component)));
foreach(var component in components)
{    
    if(BuildOptions.BuildComponent(component.Name))
        Component.Build(component);
}