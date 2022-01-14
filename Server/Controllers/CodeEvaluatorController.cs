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
                tcode = tcode.Replace("Variables." + k, "Variables['" + k + "']");
            }

            var log = new
            {
                ILog = new LogDelegate((object[] args) => { }),
                DLog = new LogDelegate((object[] args) => { }),
                WLog = new LogDelegate((object[] args) => { }),
                ELog = new LogDelegate((object[] args) => { }),
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
    }

}