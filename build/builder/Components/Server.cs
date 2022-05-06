public class Server : Component
{
    public override Type[] Dependencies => new [] { typeof(Client), typeof(FlowRunner), typeof(Node) };
    protected override void Build()
    {        
        base.Build();

        Utils.DeleteFile(BuildOptions.TempPath + "/Server/JetBrains.Annotations.dll");
        Utils.DeleteFile(BuildOptions.TempPath + "/Server/node-upgrade.bat");
        Utils.DeleteFile(BuildOptions.TempPath + "/Server/node-upgrade.sh");

        Utils.EnsureDirectoryExists(OutputPath + "/Plugins");
        Utils.CopyFile(BuildOptions.SourcePath + "/icon.ico", BuildOptions.TempPath + "/Server");
        Utils.CopyFile($"{BuildOptions.Output}/FileFlows-Node-{Globals.Version}.zip", BuildOptions.TempPath + "/Server/Nodes/");
        if(Directory.Exists(BuildOptions.SourcePath + "/build/dependencies/Plugins"))
            Utils.CopyFiles(BuildOptions.SourcePath + "/build/dependencies/Plugins", BuildOptions.TempPath + "/Server/Plugins", pattern: @"\.ffplugin$");
        
        if(Utils.DirectoryIsEmpty(OutputPath + "/Plugins"))
            File.WriteAllText(OutputPath + "/Plugins/readme.txt", "This is where plugins are installed");

        MakeInstaller();        

        // we want to make a "Server" directory inside the zip, this is so we keep the directory structure of
        // /FileFlows/Data, /FileFlows/Logs, /FileFlows/Server etc
        Directory.Move(BuildOptions.TempPath + "/Server", BuildOptions.TempPath + "/Server2"); //move this since we cant move it to itself
        Directory.CreateDirectory(BuildOptions.TempPath + "/Server/Server");
        Directory.Move(BuildOptions.TempPath + "/Server2", BuildOptions.TempPath + "/Server/Server");
        File.Move(BuildOptions.TempPath + "/Server/Server/run-server.bat",BuildOptions.TempPath + "/Server/run-server.bat");
        File.Move(BuildOptions.TempPath + "/Server/Server/run-server.sh",BuildOptions.TempPath + "/Server/run-server.sh");

        Utils.Zip(BuildOptions.TempPath + "/Server", $"{BuildOptions.Output}/FileFlows-{Globals.Version}.zip");
    }
    public void MakeInstaller()
    {
        Nsis.Build(BuildOptions.SourcePath + "/build/install/server.nsis");
        Utils.CopyFile(BuildOptions.TempPath + "/Server-Installer.exe", $"{BuildOptions.Output}/FileFlows-{Globals.Version}.exe");
    }

}