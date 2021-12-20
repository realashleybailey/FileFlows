using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixSharp;
using WixSharp.Controls;

namespace WindowsServerInstaller
{
    internal class Program
    {
        static Guid InstallGuid = new Guid("07A84D0A-C965-4AE8-958F-0800F13AC5CD");
        static Guid InstallNodeGuid = new Guid("07A84D0A-C965-4AE8-958F-0800F13AC5CE");
        static string VERSION = "0.0.0.0";
        static bool Node = false;
        static string ffmpeg = @"C:\utils\ffmpeg\ffmpeg.exe";
        static string ffPath = @"..\..\..\deploy\FileFlows";

        static public void Main(string[] args)
        {
            var rootFiles = RecursiveFileAdd(ffPath + (Node ?  "-Node": ""));
            rootFiles.Add(new Dir("Tools", new WixSharp.File(ffmpeg)));

            var dir = new Dir(@"%AppData%\FileFlows\" + (Node ? "Node" : "Server"), rootFiles.ToArray());

            var dirStartMenu = new Dir("%ProgramMenu%\\FileFlows", new ExeFileShortcut
            {
                Name = "FileFlows" + (Node ? " Node" : ""),
                Target = @"[INSTALLDIR]\FileFlows" + (Node ? "Node" : "") + ".exe",
                WorkingDirectory = @"[INSTALLDIR]"
            });

            var project = new Project("FileFlows" + (Node ? "Node" : ""), dir, dirStartMenu);

            project.LicenceFile = "eula.rtf";
            project.BannerImage = "banner.png";
            project.BackgroundImage = "background.png";
            project.GUID = Node ? InstallNodeGuid : InstallGuid;
            project.OutDir = "..\\..\\..\\deploy";
            project.OutFileName = "FileFlows" + (Node ? "Node" : "") + "-" + VERSION;
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
                items.Add(new WixSharp.File(file.FullName));

            return items;
        }
    }
}
