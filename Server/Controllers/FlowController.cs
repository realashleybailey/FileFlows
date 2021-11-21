namespace FileFlows.Server.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Helpers;
    using System.ComponentModel;
    using System.Dynamic;
    using FileFlows.Plugin;
    using FileFlows.Plugin.Attributes;

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
        public void Delete([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<Flow>(model.Uids);
        }

        [HttpGet("{uid}")]
        public Flow Get(Guid uid)
        {
            if (uid != Guid.Empty)
            {
                var flow = DbHelper.Single<Flow>(uid);
                if (flow == null)
                    return flow;


                var elements = GetElements();

                foreach (var p in flow.Parts)
                {
                    if (p.FlowElementUid.EndsWith("." + p.Name))
                        p.Name = string.Empty;
                    string icon = elements.Where(x => x.Uid == p.FlowElementUid).Select(x => x.Icon).FirstOrDefault();
                    if (string.IsNullOrEmpty(icon) == false)
                        p.Icon = icon;
                }
                return flow;
            }
            else
            {

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
                var context = new System.Runtime.Loader.AssemblyLoadContext("FlowController.Get", true);
                using var pluginLoader = new PluginHelper();
                var info = pluginLoader.GetInputFileInfo();
                if (string.IsNullOrEmpty(info.name) == false)
                {
                    flow.Parts.Add(new FlowPart
                    {
                        Name = info.name,
                        xPos = 450,
                        yPos = 50,
                        Uid = Guid.NewGuid(),
                        Type = FlowElementType.Input,
                        Outputs = 1,
                        FlowElementUid = info.fullName,
                        Icon = "far fa-file"
                    });
                }
                return flow;
            }
        }


        [HttpGet("elements")]
        public IEnumerable<FlowElement> GetElements()
        {
            var context = new System.Runtime.Loader.AssemblyLoadContext("FlowController.GetElements", true);
            using var pluginLoader = new PluginHelper();
            return pluginLoader.GetElements();
        }

        [HttpPut]
        public Flow Save([FromBody] Flow model)
        {
            if (model == null)
                throw new Exception("No model");


            if (string.IsNullOrWhiteSpace(model.Name))
                throw new Exception("ErrorMessages.NameRequired");
            model.Name = model.Name.Trim();
            bool inUse = DbHelper.GetNames<Flow>("Uid <> @1", model.Uid.ToString()).Where(x => x.ToLower() == model.Name.ToLower()).Any();
            if (inUse)
                throw new Exception("ErrorMessages.NameInUse");

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
    }

}