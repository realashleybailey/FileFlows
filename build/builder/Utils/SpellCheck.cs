class SpellCheck
{
    public static bool Execute(){
        
        Logger.ILog("Executing spellcheck");
        var result = Utils.Exec("dotnet", new [] 
        {
            BuildOptions.SourcePath + "/build/utils/spellcheck/spellcheck.dll",
            BuildOptions.SourcePath + "/Client/wwwroot/i18n"
        });
        return result.exitCode == 0;
    }
}