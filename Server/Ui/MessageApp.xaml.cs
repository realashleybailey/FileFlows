using Avalonia;

namespace FileFlows.Server.Ui;

internal class MessageApp : Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MessageBox("FileFlows is already running.", "FileFlows");
        window.Show();
    }
}
