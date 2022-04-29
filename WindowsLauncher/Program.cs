/*
 * This is just a little wrapper to launch FileFlows.Server.dll and FileFlows.Node.dll via a exe
 * These are built on linux and thus create no exe, this could be improved by making this in C or something
 * The C# version is about 10MB, which could be reduced to a few Kb at most.  Oneday
 */ 

using System.Diagnostics;

var exePath = AppDomain.CurrentDomain.BaseDirectory;
var assemblyName = Process.GetCurrentProcess().ProcessName;
    
if (assemblyName == "FileFlows")
    assemblyName = "FileFlows.Server";

var p = new Process
{
    StartInfo = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        CreateNoWindow = true
    }
};
p.StartInfo.ArgumentList.Add($"{exePath}{assemblyName}.dll");
foreach (var arg in args)
    p.StartInfo.ArgumentList.Add(arg);

p.Start();
//p.WaitForExit();