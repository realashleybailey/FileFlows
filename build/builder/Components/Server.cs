public class Server : Component
{
    public override Type[] Dependencies => new [] { typeof(Client), typeof(FlowRunner) };
    protected override void Build()
    {        
        base.Build();

        Utils.CopyFile(BuildOptions.SourcePath + "/icon.ico", BuildOptions.TempPath + "/Server");

        MakeInstaller();        
        Utils.Zip(BuildOptions.TempPath + "/Server", $"{BuildOptions.Output}/FileFlows-{Globals.Version}.zip");
    }
    public void MakeInstaller()
    {
        Nsis.Build(BuildOptions.SourcePath + "/build/install/server.nsis");
        Utils.CopyFile(BuildOptions.TempPath + "/Server-Installer.exe", $"{BuildOptions.Output}/FileFlows-{Globals.Version}.exe");
    }

}