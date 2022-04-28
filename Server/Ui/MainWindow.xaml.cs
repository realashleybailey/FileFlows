namespace FileFlows.Server.Ui;

using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;

public class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public class MainWindowViewModel : INotifyPropertyChanged
{
    string buttonText = "Click Me";

    public string ButtonText
    {
        get => buttonText;
        set 
        {
            buttonText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonText)));
        }
    }

    public string ServerUrl { get; set; }
    public string ImagePath => "FileFlows.ico";

    public event PropertyChangedEventHandler PropertyChanged;

    public void ButtonClicked() 
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo("cmd", $"/c start {ServerUrl}") { CreateNoWindow = true });
        else
        {
            Process.Start(new ProcessStartInfo("xdg-open", ServerUrl));
        }
    }

    public void ServerUrlClicked() => ButtonText = "ServerUrlClicked, Avalonia!";

    public MainWindowViewModel(){
        this.ServerUrl = $"http://{Environment.MachineName}:{WebServer.Port}/";
    }
}