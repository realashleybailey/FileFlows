using Avalonia;

namespace FileFlows.Node.Ui;

internal class MessageApp : Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MessageBox("FileFlows node is already running.", "FileFlows");
        window.Show();
    }
}
