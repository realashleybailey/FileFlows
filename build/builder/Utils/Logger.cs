public class Logger {
    public static void ILog(string message) => Log(message, "INFO");
    public static void DLog(string message) => Log(message, "DBUG");
    public static void WLog(string message) => Log(message, "WARN");
    public static void ELog(string message) => Log(message, "ERRR");

    public static void Log(string message, string type = "") 
    {
        string output = message;
        if(string.IsNullOrWhiteSpace(type) == false)
            output = type + " -> " + message;
            
        Console.WriteLine(output);
        try
        {
            File.AppendAllText(Path.Combine(BuildOptions.Output, "build.log"), output + Environment.NewLine);
        }
        catch(Exception) { }
    }
}