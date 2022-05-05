using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FileFlows.Node.Ui;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
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