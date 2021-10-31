namespace FileFlow.Server.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Shared.Models;
    using FileFlow.Server.Helpers;
    using System.ComponentModel;
    using System.Dynamic;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    [Route("/api/flow")]
    public class FlowController : Controller
    {

        [HttpGet]
        public IEnumerable<Flow> GetAll() => DbHelper.Select<Flow>();

        [HttpPut("state/{uid}")]
        public Flow SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            var flow = DbHelper.Single<Flow>(uid);
            if (flow == null)
                throw new Exception("Flow not found.");
            if (enable != null)
            {
                flow.Enabled = enable.Value;
                DbHelper.Update(flow);
            }
            return flow;
        }

        [HttpDelete]
        public void Delete([FromBody] DeleteModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<Flow>(model.Uids);
        }

        [HttpGet("{uid}")]
        public Flow Get(Guid uid)
        {
            if (uid != Guid.Empty)
                return DbHelper.Single<Flow>(uid);

            // create default flow
            var flowNames = DbHelper.GetNames<Flow>();
            Flow flow = new Flow();
            flow.Parts = new();
            flow.Name = "New Flow";
            flow.Enabled = true;
            int count = 0;
            while (flowNames.Contains(flow.Name))
            {
                flow.Name = "New Flow " + (++count);
            }
            // try find basic node
            var inputFileType = NodeHelper.GetAssemblyNodeTypes("BasicNodes")?.Where(x => x.Name == "InputFile").FirstOrDefault();
            if (inputFileType != null)
            {
                flow.Parts.Add(new FlowPart
                {
                    Name = inputFileType.Name,
                    xPos = 50,
                    yPos = 200,
                    Uid = Guid.NewGuid(),
                    Type = FlowElementType.Input,
                    Outputs = 1,
                    FlowElementUid = inputFileType.FullName
                });
            }
            return flow;
        }


        [HttpGet("elements")]
        public IEnumerable<FlowElement> GetElements()
        {
            var nodeTypes = Helpers.NodeHelper.GetNodeTypes();
            List<FlowElement> elements = new List<FlowElement>();
            foreach (var x in nodeTypes)
            {
                FlowElement element = new FlowElement();
                element.Group = x.Namespace.Substring(x.Namespace.LastIndexOf(".") + 1);
                element.Name = x.Name;
                element.Uid = x.FullName;
                element.Fields = new();
                var instance = (Node)Activator.CreateInstance(x);
                element.Inputs = instance.Inputs;
                element.Outputs = instance.Outputs;
                element.Type = instance.Type;

                // if(x.IsAssignableFrom(typeof(IConfigurableInputNode)) || x.IsAssignableFrom(typeof(IInputNode)))
                // {
                //    element.Inputs = dValue == null ? 1 : (int)dValue.Value;
                // }

                // if(x.IsAssignableFrom(typeof(IConfigurableOutputNode)) || x.IsAssignableFrom(typeof(IOutputNode)))
                // {
                //    var dValue = x.GetProperty(nameof(IOutputNode.Outputs)).GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                //    element.Outputs = dValue == null ? 1 : (int)dValue.Value;
                // }
                var model = new ExpandoObject(); ;
                var dict = (IDictionary<string, object>)model;
                element.Model = model;

                element.Fields = FormHelper.GetFields(x, dict);

                //(FileFlow.Shared.Nodes.Node)Activator.CreateInstance(x);
                elements.Add(element);
            }
            return elements;
        }

        [HttpPut]
        public Flow Save([FromBody] Flow model)
        {
            if (model == null)
                throw new Exception("No model");

            if (model.Parts?.Any() != true)
                throw new Exception("Flow.ErrorMessages.NoParts");

            int inputNodes = model.Parts.Where(x => x.Type == FlowElementType.Input).Count();
            if (inputNodes == 0)
                throw new Exception("Flow.ErrorMessages.NoInput");
            else if (inputNodes > 1)
                throw new Exception("Flow.ErrorMessages.TooManyInputNodes");

            var flow = DbHelper.Update<Flow>(model);
            return flow;
        }

        [HttpPut("{uid}/rename")]
        public void Rename([FromRoute] Guid uid, [FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            name = name.Trim();

            bool inUse = DbHelper.GetNames<Flow>("Uid <> @1", uid.ToString()).Where(x => x.ToLower() == name.ToLower()).Any();
            if (inUse)
                throw new Exception("ErrorMessages.NameInUse");

            if (uid == Guid.Empty)
                return; // renaming a new flow


            var flow = Get(uid);
            if (flow == null)
                throw new Exception("Flow not found");
            if (flow.Name == name)
                return; // name already is the requested name

            flow.Name = name;
            DbHelper.Update(flow);

            // update any object references
            var libraries = DbHelper.Select<Library>();
            foreach (var lib in libraries.Where(x => x.Flow.Uid == uid))
            {
                lib.Flow.Name = flow.Name;
                DbHelper.Update(lib);
            }
            var libraryFiles = DbHelper.Select<LibraryFile>();
            foreach (var lf in libraryFiles.Where(x => x.Flow.Uid == uid))
            {
                lf.Flow.Name = flow.Name;
                DbHelper.Update(lf);
            }
        }

        // [HttpPost("execute")]
        // public async Task<string> Execute(string input)
        // {
        //     var executor = new Helpers.FlowExecutor();
        //     executor.Flow = Get();
        //     var result = await executor.Run(input);
        //     return result.Logger.ToString();
        // }
    }

}