namespace FileFlows.Node;

/// <summary>
/// Globals variables
/// </summary>
public class Globals
{
    /// <summary>
    /// Gets the version of FileFlows
    /// </summary>
    public static string Version = "0.8.1.1427";

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

    /// <summary>
    /// Gets or sets if this node is running inside a docker container
    /// </summary>
    public static bool IsDocker { get; set; }
}
