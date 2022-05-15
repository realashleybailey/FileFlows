using System.Runtime.InteropServices;

namespace FileFlows.Server.Utils;

/// <summary>
/// Class used to show/hide the console on windows
/// </summary>
internal class WindowsConsoleManager
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    /// <summary>
    /// Hides the console
    /// </summary>
    public static void Hide()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }
    
    /// <summary>
    /// Shows the console
    /// </summary>
    public static void Show()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_SHOW);
    }
}