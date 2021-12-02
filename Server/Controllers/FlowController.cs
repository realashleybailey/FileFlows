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
    public class FlowController : ControllerStore<Flow>
    {

        [HttpGet]
        public async Task<IEnumerable<Flow>> GetAll() => (await GetDataList()).OrderBy(x => x.Name);


        [HttpPut("state/{uid}")]
        public async Task<Flow> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            var flow = await GetByUid(uid);
            if (flow == null)
                throw new Exception("Flow not found.");
            if (enable != null)
            {
                flow.Enabled = enable.Value;
                await DbManager.Update(flow);
            }
            return flow;
        }

        [HttpDelete]
        public async Task Delete([FromBody] ReferenceModel model)
        {
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            await DeleteAll(model);
        }

        [HttpGet("{uid}")]
        public async Task<Flow> Get(Guid uid)
        {
            if (uid != Guid.Empty)
            {

                var flow = await GetByUid(uid);
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
                IEnumerable<string> flowNames = await GetNames();
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
            using var pluginLoader = new PluginHelper();
            return pluginLoader.GetElements();
        }

        [HttpPut]
        public async Task<Flow> Save([FromBody] Flow model)
        {
            if (model == null)
                throw new Exception("No model");


            if (string.IsNullOrWhiteSpace(model.Name))
                throw new Exception("ErrorMessages.NameRequired");
            model.Name = model.Name.Trim();
            bool inUse = await NameInUse(model.Uid, model.Name);
            if (inUse)
                throw new Exception("ErrorMessages.NameInUse");

            if (model.Parts?.Any() != true)
                throw new Exception("Flow.ErrorMessages.NoParts");

            int inputNodes = model.Parts.Where(x => x.Type == FlowElementType.Input).Count();
            if (inputNodes == 0)
                throw new Exception("Flow.ErrorMessages.NoInput");
            else if (inputNodes > 1)
                throw new Exception("Flow.ErrorMessages.TooManyInputNodes");

            return await Update(model);
        }

        [HttpPut("{uid}/rename")]
        public async Task Rename([FromRoute] Guid uid, [FromQuery] string name)
        {

            if (uid == Guid.Empty)
                return; // renaming a new flow


            var flow = await Get(uid);
            if (flow == null)
                throw new Exception("Flow not found");
            if (flow.Name == name)
                return; // name already is the requested name

            flow.Name = name;
            await base.Update(flow);

            // update any object references
            await new LibraryFileController().UpdateFlowName(flow.Uid, flow.Name);
            var libraries = new LibraryController().UpdateFlowName(flow.Uid, flow.Name);
        }

        [HttpPost("{uid}/variables")]
        public Dictionary<string, object> GetVariables([FromBody] List<FlowPart> flowParts, [FromRoute(Name ="uid")] Guid partUid)
        {
            var variables = new Dictionary<string, object>();
            bool windows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            variables.Add("ext", ".mkv");

            variables.Add("fileName", "Filename");
            variables.Add("fileSize", 1000);
            variables.Add("fileOrigExt", ".mkv");
            variables.Add("fileOrigFileName", "OriginalFile");

            variables.Add("folderName", "FolderName");
            variables.Add("folderFullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
            variables.Add("folderOrigName", "FolderOriginalName");
            variables.Add("folderOrigFullName", windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");

            // get the connected nodes to this part
            var part = flowParts?.Where(x => x.Uid == partUid)?.FirstOrDefault();
            if (part == null)
                return variables;

            List<FlowPart> checkedParts = new List<FlowPart>();

            var parentParts = FindParts(part);
            if (parentParts.Any() == false)
                return variables;

            PluginHelper pluginHelper = new PluginHelper(); 
            foreach(var p in parentParts)
            {
                var partVariables = pluginHelper.GetPartVariables(p.FlowElementUid);
                foreach (var pv in partVariables)
                {
                    if (variables.ContainsKey(pv.Key) == false)
                        variables.Add(pv.Key, pv.Value);
                }
            }

            return variables;

            List<FlowPart> FindParts(FlowPart part)
            {
                List<FlowPart> results = new List<FlowPart>();
                if (checkedParts.Contains(part))
                    return results;


                foreach (var p in flowParts)
                {
                    if (checkedParts.Contains(p) || p == part)
                        continue;

                    if (p.OutputConnections?.Any() != true) {
                        checkedParts.Add(p);
                        continue;
                    }

                    if (p.OutputConnections.Any(x => x.InputNode == part.Uid)) 
                    {
                        results.Add(p);

                        results.AddRange(FindParts(p));
                        checkedParts.Add(p);
                    }
                }
                return results;
            }
        }
    }

}