using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileFlows.Node.Ui;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
        PointerPressed += MessageBox_PointerPressed;
    }
    public MessageBox(string message, string title = "")
    {
        InitializeComponent();
        if (string.IsNullOrWhiteSpace(title) == false)
            this.Title = title;
        DataContext = new MessageBoxViewModel(this, message);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private void MessageBox_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}


public class MessageBoxViewModel
{ 
    private MessageBox Window { get; set; }
    public string Message { get; set; } = string.Empty;

    public void Ok() => Window.Close();

    public MessageBoxViewModel(MessageBox window, string message)
    {
        this.Window = window;
        this.Message = message;
    }

}