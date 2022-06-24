using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FileFlows.ServerShared;

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
        PointerPressed += Confirm_PointerPressed;
        if (string.IsNullOrWhiteSpace(title) == false)
            this.Title = title;

        
        var dc = new ConfirmViewModel(this, message)
        {
            CustomTitle = Globals.IsWindows
        };
        DataContext = dc;

        this.ExtendClientAreaChromeHints = 
            dc.CustomTitle ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
        ExtendClientAreaToDecorationsHint = dc.CustomTitle;
        this.MaxHeight = dc.CustomTitle ? 170 : 140;
        this.Height = dc.CustomTitle ? 170 : 140;
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
    /// <summary>
    /// Gets or sets if a custom title should be rendered
    /// </summary>
    public bool CustomTitle { get; set; }
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