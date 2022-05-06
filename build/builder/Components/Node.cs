public class Node : Component
{
    public override Type[] Dependencies => new [] { typeof(FlowRunner) };
    protected override void Build()
    {        
        base.Build();

        Utils.CopyFile(BuildOptions.SourcePath + "/icon.ico", BuildOptions.TempPath + "/Node");

        MakeInstaller();        
        Utils.CopyFiles(ProjectDirectory, BuildOptions.TempPath + "/Node", false, "node\\-upgrade\\.(ps1|bat|sh)$");

        // we want to make a "Node" directory inside the zip, this is so we keep the directory structure of
        // /FileFlows/Data, /FileFlows/Logs, /FileFlows/Node etc
        Directory.Move(BuildOptions.TempPath + "/Node", BuildOptions.TempPath + "/Node/Node");
        File.Move(BuildOptions.TempPath + "/Node/Node/run-node.bat",BuildOptions.TempPath + "/Node/run-node.bat");
        File.Move(BuildOptions.TempPath + "/Node/Node/run-node.sh",BuildOptions.TempPath + "/Node/run-node.sh");

        Utils.Zip(BuildOptions.TempPath + "/Node", $"{BuildOptions.Output}/FileFlows-Node-{Globals.Version}.zip");
        Utils.DeleteFiles(BuildOptions.TempPath + "/Node", "node-upgrade.*");
    }
    public void MakeInstaller()
    {
        Nsis.Build(BuildOptions.SourcePath + "/build/install/node.nsis");
        Utils.CopyFile(BuildOptions.TempPath + "/Node-Installer.exe", $"{BuildOptions.Output}/FileFlows-Node-{Globals.Version}.exe");
    }
}