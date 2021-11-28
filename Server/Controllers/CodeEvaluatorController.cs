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
    public class CodeEvaluatorController : Controller
    {
        [HttpPost("validate")]
        public string Validate([FromBody] string code)
        {
            if (string.IsNullOrEmpty(code))
                return ""; // no code, means will run fine... i think... maybe...  depends what i do
            var sb = new StringBuilder();
            Action<object> log = (object o) =>
            {
                if (o != null)
                    sb.AppendLine(o.ToString());
            };
            var engine = new Engine()
                        .SetValue("log", log);

            var result = engine.Evaluate(code).ToObject();
            log(result);
            return sb.ToString();
        }
    }

}