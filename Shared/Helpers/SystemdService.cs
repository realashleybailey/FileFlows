using System.Diagnostics;
using System.IO;

namespace FileFlows.Shared.Helpers;


/// <summary>
/// Helper to install FileFlows node as a systemd service
/// </summary>
public class SystemdService
{
    /// <summary>
    /// Installs the service
    /// </summary>
    /// <param name="baseDirectory">the base directory for the FileFiles install, DirectoryHelper.BaseDirectory</param>
    /// <param name="isNode">if installing node or server</param>
    public static void Install(string baseDirectory, bool isNode)
    {
        SaveServiceFile(baseDirectory, isNode);
        RunService(isNode);
        Console.WriteLine("Run the following to check the status of the service: ");
        string name = isNode ? "fileflows-node" : "fileflows";
        Console.WriteLine($"sudo systemctl status {name}.service ");
    }

    /// <summary>
    /// Runs the service
    /// </summary>
    /// <param name="isNode">if installing node or server</param>
    private static void RunService(bool isNode)
    {
        string name = isNode ? "fileflows-node" : "fileflows";
        Process.Start("systemctl", $"enable {name}.service");
        Process.Start("systemctl", "daemon-reload");
        Process.Start("systemctl", $"start {name}.service");
    }

    /// <summary>
    /// Saves the service configuration file
    /// </summary>
    /// <param name="baseDirectory">the base directory for the FileFiles install, DirectoryHelper.BaseDirectory</param>
    /// <param name="isNode">if installing node or server</param>
    private static void SaveServiceFile(string baseDirectory, bool isNode)
    {
        string workingDir = Path.Combine(baseDirectory, isNode ? "Node" : "Server"); 
        string dll = Path.Combine(workingDir, isNode ? "FileFlows.Node.dll" : "FileFlows.Server.dll");
        string contents = $@"[Unit]
Description={(isNode ? "FileFlows Node" :"FileFlows")}

[Service]
WorkingDirectory={workingDir}
ExecStart=dotnet {dll} --no-gui --systemd-service
SyslogIdentifier={(isNode ? "FileFlows Node" :"FileFlows")}
Restart=on-failure
RestartSec=60

[Install]
WantedBy=multi-user.target";
        
        File.WriteAllText($"/etc/systemd/system/fileflows{(isNode ? "-node" : "")}.service", contents);
    }
}