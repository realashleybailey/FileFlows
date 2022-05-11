using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileFlows.Node.Ui;

public partial class Confirm : Window
{
    public Confirm()
    {
        InitializeComponent();
        PointerPressed += Confirm_PointerPressed;
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
    
    private void Confirm_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this);
        {
            BeginMoveDrag(e);
        }
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