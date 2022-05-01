public class Server : Component
{
    public override Type[] Dependencies => new [] { typeof(Client), typeof(FlowRunner) };
    protected override void Build()
    {        
        base.Build();

        Utils.DeleteFile(BuildOptions.TempPath + "/Server/JetBrains.Annotations.dll");

        Utils.EnsureDirectoryExists(OutputPath + "/Plugins");
        File.WriteAllText(OutputPath + "/Plugins/readme.txt", "This is where plugins are installed");
        Utils.CopyFile(BuildOptions.SourcePath + "/icon.ico", BuildOptions.TempPath + "/Server");
        if(Directory.Exists(BuildOptions.SourcePath + "/build/dependencies/Plugins"))
            Utils.CopyFiles(BuildOptions.SourcePath + "/build/dependencies/Plugins", BuildOptions.TempPath + "/Server/Plugins", pattern: @"\.ffplugin$");
        
        MakeInstaller();        
        Utils.Zip(BuildOptions.TempPath + "/Server", $"{BuildOptions.Output}/FileFlows-{Globals.Version}.zip");
    }
    public void MakeInstaller()
    {
        Nsis.Build(BuildOptions.SourcePath + "/build/install/server.nsis");
        Utils.CopyFile(BuildOptions.TempPath + "/Server-Installer.exe", $"{BuildOptions.Output}/FileFlows-{Globals.Version}.exe");
    }

}