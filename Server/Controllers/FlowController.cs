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
        public IEnumerable<Flow> GetAll()
        {
            if (Globals.Demo)
            {
                return Enumerable.Range(1, 10).Select(x => new Flow
                {
                    Uid = Guid.NewGuid(),
                    Name = "Demo Flow " + x,
                    Enabled = x < 5
                });
            }
            return DbHelper.Select<Flow>();
        }


        [HttpPut("state/{uid}")]
        public Flow SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
        {
            if (Globals.Demo)
                return new Flow { Uid = uid, Enabled = enable == true };

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
            if (Globals.Demo)
                return;

            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<Flow>(model.Uids);
        }

        [HttpGet("{uid}")]
        public Flow Get(Guid uid)
        {
            if (uid != Guid.Empty)
            {
                if (Globals.Demo)
                {
                    string json = "{\"Enabled\":1,\"Parts\":[{\"Uid\":\"10c99731-370d-41b6-b400-08d003e6e843\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.VideoNodes.VideoFile\",\"xPos\":411,\"yPos\":18,\"Icon\":\"fas fa-video\",\"Inputs\":0,\"Outputs\":1,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"38e28c04-4ce7-4bcf-90f3-79ed0796f347\"}],\"Type\":0,\"Model\":{}},{\"Uid\":\"3121dcae-bfb8-4c37-8871-27618b29beb4\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.VideoNodes.Video_H265_AC3\",\"xPos\":403,\"yPos\":310,\"Icon\":\"far fa-file-video\",\"Inputs\":1,\"Outputs\":2,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"7363e1d1-2cc3-444c-b970-a508e7ef3d42\"},{\"Input\":1,\"Output\":2,\"InputNode\":\"7363e1d1-2cc3-444c-b970-a508e7ef3d42\"}],\"Type\":2,\"Model\":{\"Language\":\"eng\",\"Crf\":21,\"NvidiaEncoding\":true,\"Threads\":0,\"Name\":\"\",\"NormalizeAudio\":false}},{\"Uid\":\"7363e1d1-2cc3-444c-b970-a508e7ef3d42\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.BasicNodes.File.MoveFile\",\"xPos\":404,\"yPos\":489,\"Icon\":\"fas fa-file-export\",\"Inputs\":1,\"Outputs\":1,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"bc8f30c0-a72e-47a4-94fc-7543206705b9\"}],\"Type\":2,\"Model\":{\"DestinationPath\":\"/media/downloads/converted/tv\",\"MoveFolder\":true,\"DeleteOriginal\":true}},{\"Uid\":\"38e28c04-4ce7-4bcf-90f3-79ed0796f347\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.VideoNodes.DetectBlackBars\",\"xPos\":411,\"yPos\":144,\"Icon\":\"fas fa-film\",\"Inputs\":1,\"Outputs\":2,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"3121dcae-bfb8-4c37-8871-27618b29beb4\"},{\"Input\":1,\"Output\":2,\"InputNode\":\"3121dcae-bfb8-4c37-8871-27618b29beb4\"}],\"Type\":3,\"Model\":{}},{\"Uid\":\"bc8f30c0-a72e-47a4-94fc-7543206705b9\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.BasicNodes.File.DeleteSourceDirectory\",\"xPos\":404,\"yPos\":638,\"Icon\":\"far fa-trash-alt\",\"Inputs\":1,\"Outputs\":2,\"OutputConnections\":null,\"Type\":2,\"Model\":{\"IfEmpty\":true,\"IncludePatterns\":[\"mkv\",\"mp4\",\"divx\",\"avi\"]}}]}";

                    var serializerOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        Converters = { new BoolConverter() }
                    };
                    return System.Text.Json.JsonSerializer.Deserialize<Flow>(json, serializerOptions);
                }

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
                IEnumerable<string> flowNames = new string[] { };
                if(Globals.Demo == false)
                    flowNames = DbHelper.GetNames<Flow>();
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
        public Flow Save([FromBody] Flow model)
        {
            if (Globals.Demo)
                return model;

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

            if (Globals.Demo)
                return;

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