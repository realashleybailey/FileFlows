public abstract class Component 
{
    public virtual string Name => this.GetType().Name;

    public virtual Type[] Dependencies => new Type[] { };

    public virtual string ProjectDirectory => BuildOptions.SourcePath + "/" + Name;
    public virtual string ProjectFile => ProjectDirectory + "/" + Name + ".csproj";
    public virtual string OutputPath => BuildOptions.TempPath + "/" + this.Name;

    public virtual void Execute()
    {
        if(BuildOptions.SetVersions)
            SetVersion();
        CopyDependencies();
        Build();
    }

    protected virtual void Clean()
    {
        Utils.DeleteDirectoryIfExists(ProjectDirectory + "/bin");
        Utils.DeleteDirectoryIfExists(ProjectDirectory + "/obj");
        //Utils.DeleteDirectoryIfExists(OutputPath);
    }

    protected virtual void Build()
    {        
        DotNet.Build(ProjectFile, new DotNet.BuildSettings{
            OutputDirectory = OutputPath,
            Configuration = "Release"
        });
    }

    protected virtual void SetVersion()
    {        
        Utils.RegexReplace(ProjectFile, "<Version>[^<]+</Version>", $"<Version>{Globals.Version}</Version>");
        Utils.RegexReplace(ProjectFile, "<ProductVersion>[^<]+</ProductVersion>", $"<ProductVersion>{Globals.Version}</ProductVersion>");
        Utils.RegexReplace(ProjectFile, "<Copyright>[^<]+</Copyright>", $"<Copyright>{Globals.Copyright}</Copyright>");    
        string globalcs = ProjectDirectory + "/Globals.cs";
        if(File.Exists(globalcs))
        {
            Utils.RegexReplace(globalcs, Regex.Escape("string Version = \"" + @"([\d]+\.){3}[\d]+") + "\"", $"string Version =\"{Globals.Version}\"");
        }
    }

    static Dictionary<string, Component> Instances = new Dictionary<string, Component>();

    public static T GetInstance<T>() where T : Component {
        var instance = GetInstance(typeof(T));
        return (T)instance;
    }
    static Component GetInstance(Type type) {
        string key = type.Name;
        if(Instances.ContainsKey(key))
            return (Component)Instances[key];
        var instance = (Component)Activator.CreateInstance(type);
        Instances.Add(key, instance);
        return instance;
    }

    public static void Build<T>() => Build(typeof(T));
    
    public static void Build(Type type)
    {
        var instance = GetInstance(type);
        instance.Clean(); // clean before dependencies are built incase they build to this folder
        foreach(var dependency in instance.Dependencies ?? new Type[] {}){
            if(Instances.ContainsKey(dependency.Name))
                continue;
            Build(dependency);
        }
        instance.Execute();
    }

    protected virtual void CopyDependencies()
    {
        if(this.Dependencies?.Any() != true)
            return;
        foreach(var dependency in Dependencies)
        {
            var instance = GetInstance(dependency);
            if(instance.OutputPath.StartsWith(this.OutputPath) == false)
            {
                if(dependency.Name == nameof(FlowRunner))
                    Utils.CopyFiles(instance.OutputPath, this.OutputPath + "/FileFlows-Runner");
                else
                    Utils.CopyFiles(instance.OutputPath, this.OutputPath);
            }
        }
    }
}