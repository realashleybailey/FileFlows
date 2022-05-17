using FileFlows.ServerShared.Helpers;

namespace FileFlows.Node.Utils;

/// <summary>
/// Helper to install FileFlows node as a systemd service
/// </summary>
public class SystemdService
{
    /// <summary>
    /// Installs the service
    /// </summary>
    /// <param name="dotnet">the location of dotnet</param>
    public static void Install(string dotnet)
    {
        if (string.IsNullOrWhiteSpace(dotnet))
        {
            Console.WriteLine("Dotnet location not supplied, you must supply this to install the service");
            return;
        }
        SaveServiceFile(dotnet);
        RunService();
        Console.WriteLine("Run the following to check the status of the service: ");
        Console.WriteLine("sudo systemctl status fileflows-node.service ");
    }

    /// <summary>
    /// Runs the service
    /// </summary>
    private static void RunService()
    {
        Process.Start("systemctl", "enable fileflows-node.service");
        Process.Start("systemctl", "daemon-reload");
        Process.Start("systemctl", "start fileflows-node.service");
    }

    /// <summary>
    /// Saves the service configuration file
    /// </summary>
    /// <param name="dotnet">the location of dotnet</param>
    private static void SaveServiceFile(string dotnet)
    {
        string workingDir = Path.Combine(DirectoryHelper.BaseDirectory, "Node"); 
        string dll = Path.Combine(workingDir, "FileFlows.Node.dll");
        string contents = $@"[Unit]
Description=FileFlows Node

[Service]
ExecStart={dotnet} {dll} --no-gui
SyslogIdentifier=FileFlows Node
WorkingDirectory={workingDir}
User=root
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target";
        
        File.WriteAllText("/etc/systemd/system/fileflows-node.service", contents);
    }
}