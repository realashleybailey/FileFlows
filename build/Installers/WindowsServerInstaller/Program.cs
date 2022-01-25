using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using WixSharp;
using WixSharp.CommonTasks;
using Microsoft.Deployment.WindowsInstaller;

internal class Program
{
    static Guid InstallGuid = new Guid("07A84D0A-C965-4AE8-958F-0800F13AC5CD");
    static Guid InstallNodeGuid = new Guid("07A84D0A-C965-4AE8-958F-0800F13AC5CE");
    static string VERSION = "0.2.1.366";
    static bool Node = false;
    static string ffmpeg = @"C:\utils\ffmpeg\ffmpeg.exe";
    static string ffPath = @"..\..\..\deploy\FileFlows";

    static public void Main(string[] args)
    {
        var rootFiles = RecursiveFileAdd(ffPath + (Node ? "-Node" : ""));
        rootFiles.Add(new Dir("Tools", new WixSharp.File(ffmpeg)));

        var dir = new Dir(@"%AppData%\FileFlows\" + (Node ? "Node" : "Server"), rootFiles.ToArray());

        var dirStartMenu = new Dir("%ProgramMenu%\\FileFlows", new ExeFileShortcut
        {
            Name = "FileFlows" + (Node ? " Node" : ""),
            Target = @"[INSTALLDIR]\FileFlows" + (Node ? "Node" : "") + ".exe",
            WorkingDirectory = @"[INSTALLDIR]"
        });


        Project project;
        if (Node == false)
        {
            project = new Project("FileFlows",
                dir, dirStartMenu,
                new ElevatedManagedAction(CustonActions.StopProcesses, Return.ignore, When.Before, Step.InstallValidate, Condition.NOT_BeingRemoved)
                {
                    Execute = Execute.immediate                    
                },
                new ManagedAction(CustonActions.StartFileFlowsServerSilent, Return.ignore, When.After, Step.InstallFinalize, Condition.Silent),
                new ManagedAction(CustonActions.StartFileFlowsServer, Return.ignore, When.After, Step.InstallFinalize, Condition.NOT_Silent)
            );
        }
        else
        {
            project = new Project("FileFlows Node",
                dir, dirStartMenu,
                new ElevatedManagedAction(CustonActions.StopProcesses, Return.ignore, When.Before, Step.InstallValidate, Condition.NOT_BeingRemoved)
                {
                    Execute = Execute.immediate
                },
                new ManagedAction(CustonActions.StartFileFlowsNode, Return.ignore, When.After, Step.InstallFinalize, Condition.Always)
            );
        }

        project.ResolveWildCards().FindFile(f => f.Name.EndsWith("FileFlows" + (Node ? "Node" : "") + ".exe")).First()
            .Shortcuts = new[]{
            new FileShortcut("FileFlows" + (Node ? " Node" :""), @"%AppData%\Microsoft\Windows\Start Menu\Programs\Startup")
            {
                Arguments = "--silent"
            }
        };

        project.MajorUpgrade = new MajorUpgrade
        {
            AllowDowngrades = true,
            Disallow = false,
            Schedule = UpgradeSchedule.afterInstallInitialize
        };

        foreach (var media in project.Media)
            media.CompressionLevel = CompressionLevel.high;

        project.LicenceFile = "eula.rtf";
        project.BannerImage = "banner.png";
        project.BackgroundImage = "background.png";
        project.GUID = Node ? InstallNodeGuid : InstallGuid;
        project.OutDir = "..\\..\\..\\deploy";
        project.OutFileName = "FileFlows" + (Node ? "-Node" : "") + "-" + VERSION;
        project.Version = new Version(VERSION);

        Compiler.BuildMsi(project);
    }

    private static List<WixEntity> RecursiveFileAdd(string dir)
    {
        var di = new DirectoryInfo(dir);
        List<WixEntity> items = new List<WixEntity>();
        foreach (var subdir in di.GetDirectories())
        {
            WixEntity sd = new Dir(subdir.Name, RecursiveFileAdd(subdir.FullName).ToArray());
            items.Add(sd);
        }
        foreach (var file in di.GetFiles())
        {
            var wixFile = new WixSharp.File(file.FullName);
            items.Add(wixFile);
        }

        return items;
    }
}

public class CustonActions
{
    [CustomAction]
    public static ActionResult StopProcesses(Session session)
    {
        foreach (string name in new string[] { "FileFlows.exe", "FileFlowsNode.exe" })
        {
            // request stop
            Process.Start(new ProcessStartInfo("taskkill.exe", "/im " + name) { CreateNoWindow = true, UseShellExecute = false });
        }
        foreach (string name in new string[] { "FileFlows.exe", "FileFlowsNode.exe", "FileFlows.Server.exe", "FileFlows.Node.exe" })
        {
            // force stop
            Process.Start(new ProcessStartInfo("taskkill.exe", "/f /im " + name) { CreateNoWindow = true, UseShellExecute = false });
        }
        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult StartFileFlowsServer(Session session)
    {        
        System.Diagnostics.Process.Start(new ProcessStartInfo(session["INSTALLDIR"] + @"\FileFlows.exe", "--installer")
        {
            WorkingDirectory = session["INSTALLDIR"]
        });
        return ActionResult.Success;
    }
    [CustomAction]
    public static ActionResult StartFileFlowsServerSilent(Session session)
    {
        System.Diagnostics.Process.Start(new ProcessStartInfo(session["INSTALLDIR"] + @"\FileFlows.exe", "--installer --silent")
        {
            WorkingDirectory = session["INSTALLDIR"]
        });
        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult StartFileFlowsNode(Session session)
    {
        System.Diagnostics.Process.Start(new ProcessStartInfo(session["INSTALLDIR"] + @"\FileFlowsNode.exe", "--installer")
        {
            WorkingDirectory = session["INSTALLDIR"]
        });
        return ActionResult.Success;
    }
}

//public class CustonActions
//{
//    [CustomAction]
//    public static ActionResult FileFlowsAction(Session session)
//    {
//        System.Diagnostics.Process.Start(session["INSTALLDIR"] + @"\FileFlows.exe");
//        return ActionResult.Success;
//    }
//    [CustomAction]
//    public static ActionResult FileFlowsNodeAction(Session session)
//    {
//        System.Diagnostics.Process.Start(session["INSTALLDIR"] + @"\FileFlowsNode.exe");
//        return ActionResult.Success;
//    }


//    static readonly List<string> runtimes = new List<string>()
//    {
//        "Microsoft.NETCore.App",//.NET Runtime
//        "Microsoft.AspNetCore.App",//ASP.NET Core Runtime
//        "Microsoft.WindowsDesktop.App",//.NET Desktop Runtime
//    };

//    [CustomAction]
//    public static ActionResult CheckVersion(Session session)
//    {
//        var minVersion = new Version(6, 0, 1);
//        var command = "/c dotnet --list-runtimes";// /c is important here
//        var output = string.Empty;
//        using (var p = new Process())
//        {
//            p.StartInfo = new ProcessStartInfo()
//            {
//                FileName = "cmd.exe",
//                Arguments = command,
//                UseShellExecute = false,
//                RedirectStandardError = true,
//                RedirectStandardOutput = true,
//                CreateNoWindow = true,
//            };
//            p.Start();
//            while (!p.StandardOutput.EndOfStream)
//            {
//                output += $"{p.StandardOutput.ReadLine()}{Environment.NewLine}";
//            }
//            p.WaitForExit();
//            if (p.ExitCode != 0)
//            {
//                session["DOTNETCORE1"] = "0";
//                return ActionResult.Success;
//                //throw new Exception($"{p.ExitCode}:{ p.StandardError.ReadToEnd()}");
//            }
//            session["DOTNETCORE1"] = (GetLatestVersionOfRuntime(runtimes[0], output) < minVersion) ? "0" : "1";
//            return ActionResult.Success;
//        }
//    }

//    private static Version GetLatestVersionOfRuntime(string runtime, string runtimesList)
//    {
//        var latestLine = runtimesList.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains(runtime)).OrderBy(x => x).LastOrDefault();
//        if (latestLine != null)
//        {
//            Regex pattern = new Regex(@"\d+(\.\d+)+");
//            Match m = pattern.Match(latestLine);
//            string versionValue = m.Value;
//            if (Version.TryParse(versionValue, out var version))
//            {
//                return version;
//            }
//        }
//        return null;
//    }
//}
