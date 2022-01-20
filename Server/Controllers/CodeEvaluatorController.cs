namespace FileFlows.Server.Controllers
{
    using System;
    using System.Text;
    using System.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;
    using Jint.Runtime;
    using Jint.Native.Object;
    using Jint;
    using System.Text.RegularExpressions;

    [Route("/api/code-eval")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CodeEvaluatorController : Controller
    {

        delegate void LogDelegate(params object[] values);

        [HttpPost("validate")]
        public string Validate([FromBody] ValidateModel model)
        {
            if (string.IsNullOrEmpty(model?.Code))
                return ""; // required will catch this


            // replace Variables. with dictionary notation
            string tcode = model.Code;
            foreach (string k in model.Variables.Keys.OrderByDescending(x => x.Length))
            {
                // replace Variables.Key or Variables?.Key?.Subkey etc to just the variable
                // so Variables.file?.Orig.Name, will be replaced to Variables["file.Orig.Name"] 
                // since its just a dictionary key value 
                string keyRegex = @"Variables(\?)?\." + k.Replace(".", @"(\?)?\.");
                tcode = Regex.Replace(tcode, keyRegex, "Variables['" + k + "']");
            }

            var logger = new TestLogger();
            var log = new
            {
                ILog = new LogDelegate(logger.ILog),
                DLog = new LogDelegate(logger.DLog),
                WLog = new LogDelegate(logger.WLog),
                ELog = new LogDelegate(logger.ELog)
            };
            var engine = new Engine(options =>
            {
                options.LimitMemory(4_000_000);
                options.MaxStatements(500);
            })
            .SetValue("Logger", log)
            .SetValue("Variables", model.Variables)
            .SetValue("Flow", new Plugin.NodeParameters(string.Empty, null, false, String.Empty));


            try
            {
                engine.Evaluate(tcode).ToObject();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return String.Empty;
        }


        public class ValidateModel
        {
            public string Code { get; set; }
            public Dictionary<string, object> Variables { get; set; }
        }

        private class TestLogger : FileFlows.Plugin.ILogger
        {
            public void DLog(params object[] args)
            {
            }

            public void ELog(params object[] args)
            {
            }

            public void ILog(params object[] args)
            {
            }

            public void WLog(params object[] args)
            {
            }
        }
    }

}