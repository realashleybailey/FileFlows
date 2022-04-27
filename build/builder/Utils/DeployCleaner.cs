public class DeployCleaner {
    public static void Execute()
    {
        var output = new DirectoryInfo(BuildOptions.Output);
        if(output.Exists)
        {
            var subDirs = output.GetDirectories();
            foreach(var di in subDirs)
            {
                if(di.Name.ToLower() == "plugins")
                    continue;
                di.Delete(true);
            }         
             
            var logFile = new FileInfo(Path.Combine(BuildOptions.Output, "build.log"));
            if(logFile.Exists)
                logFile.Delete();  
        }
        

        Utils.DeleteDirectoryIfExists(BuildOptions.TempPath);
    }
}