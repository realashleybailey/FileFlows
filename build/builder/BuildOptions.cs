using System.Runtime.InteropServices;
using System.Reflection;

public class BuildOptions
{
    public static string SourcePath = "../..";

    public static string Output { get; private set ; } = "../deploy";
    public static string TempPath { get; private set ; } = "../temp";

    public static bool BuildAll => string.IsNullOrWhiteSpace(Target);
    public static bool IsWindows { get; set; }
    public static bool IsLinux { get; set; }

    [CommandLineArgumentAttribute]
    public static string Target { get; set; }

    [CommandLineArgumentAttribute("docker")]
    public static bool IsDocker { get; set; }

    [CommandLineArgumentAttribute("installer")]
    public static bool BuildInstaller { get; set; }

    [CommandLineArgumentAttribute]
    public static bool SetVersions { get; set; }

    public static void Initialize(string[] args)
    {
        BuildOptions.IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        BuildOptions.IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        var props = typeof(BuildOptions).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        foreach(var prop in props)
        {
            var cla = prop.GetCustomAttribute<CommandLineArgumentAttribute>();
            if(cla == null)
                continue;
            string name = cla.Name?.EmptyAsNull() ?? prop.Name;
            string strValue = GetArgument(args, name);
            if(string.IsNullOrWhiteSpace(strValue))
                continue;
            if(prop.PropertyType == typeof(bool))
                prop.SetValue(null, strValue.ToLower() == "true" || strValue == "1");
            else if(prop.PropertyType == typeof(string))
                prop.SetValue(null, strValue);
        }

        if(IsDocker)
            SetVersions = true;
        
        if(BuildOptions.BuildAll)
            BuildOptions.BuildInstaller = BuildOptions.IsWindows;
    }

    private static string GetArgument(string[] args, string name)
    {
        if(args == null)
            return string.Empty;
        for(int i=0;i<args.Length - 1;i++)
        {
            if(args[i].ToLower() == "--" + name.ToLower())
                return args[i + 1] ?? string.Empty;
        }
        return string.Empty;
    }

    public static bool BuildComponent(string component){
        if(BuildAll == false)
            return Target.ToLower().Replace("fileflows", "") == component.ToLower().Replace("fileflows", "");
        if(component.ToLower().Contains("installer"))
            return BuildInstaller;
        return true;
    }
}