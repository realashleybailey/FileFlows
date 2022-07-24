using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jint;
using Jint.Runtime;

namespace FileFlows.ScriptExecution;

/// <summary>
/// A Javascript code executor
/// </summary>
/// 
public class Executor
{
    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);
    
    /// <summary>
    /// Gets or sets the variables that will be passed into the executed code
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the logger for the code execution
    /// </summary>
    public Logger Logger { get; set; }
    
    /// <summary>
    /// Gets or sets the code to execute
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets the HTTP client to be used in the code execution
    /// </summary>
    public HttpClient HttpClient { get; set; }
    
    
    /// <summary>
    /// Gets or sets the additional arguments that will be passed into the code execution
    /// </summary>
    public Dictionary<string, object> AdditionalArguments { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the directory where shared modules will be loaded from
    /// </summary>
    public string SharedDirectory { get; set; }

    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <returns>the output to be called next</returns>
    public object Execute()
    {
        if (string.IsNullOrEmpty(Code))
            return false; // no code, flow cannot continue doesnt know what to do
        try
        {
            // replace Variables. with dictionary notation
            string tcode = Code;
            foreach (string k in Variables.Keys.OrderByDescending(x => x.Length))
            {
                // replace Variables.Key or Variables?.Key?.Subkey etc to just the variable
                // so Variables.file?.Orig.Name, will be replaced to Variables["file.Orig.Name"] 
                // since its just a dictionary key value 
                string keyRegex = @"Variables(\?)?\." + k.Replace(".", @"(\?)?\.");
                
                object? value = Variables[k];
                if (value is JsonElement jElement)
                {
                    if (jElement.ValueKind == JsonValueKind.String)
                        value = jElement.GetString();
                    if (jElement.ValueKind == JsonValueKind.Number)
                        value = jElement.GetInt64();
                }

                tcode = Regex.Replace(tcode, keyRegex, "Variables['" + k + "']");
            }

            tcode = tcode.Replace("Flow.Execute(", "Execute(");
                

            var sb = new StringBuilder();
            var log = new
            {
                ILog = new LogDelegate(Logger.ILog),
                DLog = new LogDelegate(Logger.DLog),
                WLog = new LogDelegate(Logger.WLog),
                ELog = new LogDelegate(Logger.ELog),
            };
            var engine = new Engine(options =>
            {
                options.AllowClr();
                options.EnableModules(SharedDirectory);
            })
            .SetValue("Logger", Logger)
            .SetValue("Variables", Variables)
            .SetValue("Sleep", (int milliseconds) => Thread.Sleep(milliseconds))
            .SetValue("http", HttpClient)
            .SetValue("MissingVariable", (string variableName) => {
                Logger.ELog("MISSING VARIABLE: " + variableName + Environment.NewLine + $"The required variable '{variableName}' is missing and needs to be added via the Variables page.");
                throw new MissingVariableException();
            })
            .SetValue("Hostname", Environment.MachineName)
            // .SetValue("Execute", (object eArgs) => {
            //     string json = JsonSerializer.Serialize(eArgs);
            //     var jsonOptions = new JsonSerializerOptions()
            //     {
            //         PropertyNameCaseInsensitive = true
            //     };
            //     var eeARgs = JsonSerializer.Deserialize<ExecuteArgs>(JsonSerializer.Serialize(eArgs), jsonOptions);
            //     var result = args.Execute(eeARgs);
            //     Logger.ILog("result:", result);
            //     return result;
            //  })
            .SetValue(nameof(FileInfo), new Func<string, FileInfo>((string file) => new FileInfo(file)))
            .SetValue(nameof(DirectoryInfo), new Func<string, DirectoryInfo>((string path) => new DirectoryInfo(path))); ;
            
            foreach (var arg in AdditionalArguments ?? new ())
                engine.SetValue(arg.Key, arg.Value);
            
            engine.AddModule("Script", tcode);
            var ns = engine.ImportModule("Script");
            var result = ns.Get("result");            
            try
            {
                if(result != null)
                {
                    int num = (int)result.AsNumber();

                    Logger.ILog("Script result: " + num);
                    return num;
                }
            }
            catch(Exception) { }

            return true;
        }
        catch(JavaScriptException ex)
        {
            // print out the code block for debugging
            int lineNumber = 0;
            var lines = Code.Split('\n');
            string pad = "D" + (lines.ToString().Length);
            Logger.DLog("Code: " + Environment.NewLine +
                string.Join("\n", lines.Select(x => (++lineNumber).ToString("D3") + ": " + x)));

            Logger.ELog($"Failed executing script: {ex.Message}");
            return false;

        }
        catch (Exception ex)
        {
            Logger.ELog("Failed executing script: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return false;
        }
    }
}


public class MissingVariableException:Exception
{

}