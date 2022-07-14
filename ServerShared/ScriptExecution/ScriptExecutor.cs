using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Plugin.Models;
using Jint;
using Jint.Runtime;

namespace FileFlows.ServerShared.ScriptExecution;

/// <summary>
/// A Javascript code executor
/// </summary>
/// 
public class ScriptExecutor:IScriptExecutor
{
    /// <summary>
    /// Delegate used by the executor so log messages can be passed from the javascript code into the flow runner
    /// </summary>
    /// <param name="values">the parameters for the logger</param>
    delegate void LogDelegate(params object[] values);

    /// <summary>
    /// Executes javascript
    /// </summary>
    /// <param name="execArgs">the execution arguments</param>
    /// <returns>the output to be called next</returns>
    public int Execute(ScriptExecutionArgs execArgs)
    {
        if (string.IsNullOrEmpty(execArgs?.Code))
            return -1; // no code, flow cannot continue doesnt know what to do
        var args = execArgs.Args;
        try
        {
            // replace Variables. with dictionary notation
            string tcode = execArgs.Code;
            foreach (string k in args.Variables.Keys.OrderByDescending(x => x.Length))
            {
                // replace Variables.Key or Variables?.Key?.Subkey etc to just the variable
                // so Variables.file?.Orig.Name, will be replaced to Variables["file.Orig.Name"] 
                // since its just a dictionary key value 
                string keyRegex = @"Variables(\?)?\." + k.Replace(".", @"(\?)?\.");


                object? value = args.Variables[k];
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
                ILog = new LogDelegate(args.Logger.ILog),
                DLog = new LogDelegate(args.Logger.DLog),
                WLog = new LogDelegate(args.Logger.WLog),
                ELog = new LogDelegate(args.Logger.ELog),
            };
            var engine = new Engine(options =>
            {
                // remove limits due to issue reported on discord
                //options.LimitMemory(4_000_000);
                //options.MaxStatements(500);
            })
            .SetValue("Logger", args.Logger)
            .SetValue("Variables", args.Variables)
            .SetValue("Sleep", (int milliseconds) => Thread.Sleep(milliseconds))
            .SetValue("Flow", args)
            .SetValue("Hostname", Environment.MachineName)
            .SetValue("Execute", (object eArgs) => {
                args.Logger.ILog("eArgsType:", eArgs.GetType().FullName);
                args.Logger.ILog("eArgsType Json:", JsonSerializer.Serialize(eArgs));
                args.Logger.ILog("eArgsType:", eArgs);
                string json = JsonSerializer.Serialize(eArgs);
                var jsonOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                };
                var eeARgs = JsonSerializer.Deserialize<ExecuteArgs>(JsonSerializer.Serialize(eArgs),jsonOptions);
                var result = args.Execute(eeARgs);
                args.Logger.ILog("result:", result);
                return result;
             })
            .SetValue(nameof(FileInfo), new Func<string, FileInfo>((string file) => new FileInfo(file)))
            .SetValue(nameof(DirectoryInfo), new Func<string, DirectoryInfo>((string path) => new DirectoryInfo(path))); ;
            foreach (var arg in execArgs.AdditionalArguments ?? new ())
                engine.SetValue(arg.Key, arg.Value);

            var result = int.Parse(engine.Evaluate(tcode).ToObject().ToString());
            return result;
        }
        catch(JavaScriptException ex)
        {
            // print out the code block for debugging
            int lineNumber = 0;
            var lines = execArgs.Code.Split('\n');
            string pad = "D" + (lines.ToString().Length);
            args.Logger.DLog("Code: " + Environment.NewLine +
                string.Join("\n", lines.Select(x => (++lineNumber).ToString("D3") + ": " + x)));

            args.Logger?.ELog($"Failed executing script [{ex.LineNumber}, {ex.Column}]: {ex.Message}");
            return -1;

        }
        catch (Exception ex)
        {
            args.Logger?.ELog("Failed executing script: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return -1;
        }
    }
}