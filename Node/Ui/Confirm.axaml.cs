using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileFlows.Node.Ui;

public partial class Confirm : Window
{
    public Confirm()
    {
        InitializeComponent();
    }
    public Confirm(string message, string title = "")
    {
        InitializeComponent();
        if (string.IsNullOrWhiteSpace(title) == false)
            this.Title = title;
        DataContext = new ConfirmViewModel(this, message);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}


public class ConfirmViewModel
{ 
    private Confirm Window { get; set; }
    
    public string Message { get; set; } = string.Empty;

    public void Yes() => Window.Close(true);

    public void No() => Window.Close(false);

    public ConfirmViewModel(Confirm window, string message)
    {
        this.Window = window;
        this.Message = message;
    }

}