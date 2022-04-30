namespace FileFlows.Node;

/// <summary>
/// Globals variables
/// </summary>
public class Globals
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    public static string Version = "0.2.1.367";

    /// <summary>
    /// Gets if this is running on Windows
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    /// <summary>
    /// Gets if this is running on linux
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux); 
    
    /// <summary>
    /// Gets if this is running on Mac
    /// </summary>
    public static bool IsMac => RuntimeInformation.IsOSPlatform(OSPlatform.OSX); 
}
