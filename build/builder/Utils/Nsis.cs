public class Nsis 
{
    public static void Build(string file)
    {
        string parameters = $"\"{file}\"";

        var result = Utils.Exec(BuildOptions.IsWindows ? "makensis.exe" : "makensis", parameters);
        if(result.exitCode != 0)
            throw new Exception("Build Failed!");
    }
    
}