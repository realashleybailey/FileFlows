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
    using FileFlows.Server.Models;
    using FileFlows.ServerShared.Helpers;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using FileFlows.Shared;

    [Route("/api/flow")]
    public class FlowController : ControllerStore<Flow>
    {
        const int DEFAULT_XPOS = 450;
        const int DEFAULT_YPOS = 50;

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
                    string icon = elements?.Where(x => x.Uid == p.FlowElementUid)?.Select(x => x.Icon)?.FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrEmpty(icon) == false)
                        p.Icon = icon;
                    p.Label = Translater.TranslateIfHasTranslation($"Flow.Parts.{p.FlowElementUid.Substring(p.FlowElementUid.LastIndexOf(".") + 1)}.Label", string.Empty);
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
                        xPos = DEFAULT_XPOS,
                        yPos = DEFAULT_YPOS,
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
        public async Task<Flow> Save([FromBody] Flow model, [FromQuery] bool uniqueName = false)
        {
            if (model == null)
                throw new Exception("No model");


            if (string.IsNullOrWhiteSpace(model.Name))
                throw new Exception("ErrorMessages.NameRequired");
            model.Name = model.Name.Trim();
            if (uniqueName == false)
            {
                bool inUse = await NameInUse(model.Uid, model.Name);
                if (inUse)
                    throw new Exception("ErrorMessages.NameInUse");
            }
            else
            {
                model.Name = await GetNewUniqueName(model.Name);
            }            

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
        public Dictionary<string, object> GetVariables([FromBody] List<FlowPart> flowParts, [FromRoute(Name ="uid")] Guid partUid, [FromQuery] bool isNew = false)
        {
            var variables = new Dictionary<string, object>();
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
            bool dir = flowParts?.Any(x => x.FlowElementUid.EndsWith("InputDirectory")) == true;

            if (dir)
            {
                variables.Add("folder.Name", "FolderName");
                variables.Add("folder.FullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
                variables.Add("folder.Date", DateTime.Now);
                variables.Add("folder.Date.Day", DateTime.Now.Day);
                variables.Add("folder.Date.Month", DateTime.Now.Month);
                variables.Add("folder.Date.Year", DateTime.Now.Year);
                variables.Add("folder.OrigName", "FolderOriginalName");
                variables.Add("folder.OrigFullName", windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");
            }
            else
            {
                variables.Add("ext", ".mkv");
                variables.Add("file.Name", "Filename");
                variables.Add("file.Extension", ".mkv");
                variables.Add("file.Size", 1000);
                variables.Add("file.FullName", "/media/temp/randomfile.ext");
                variables.Add("file.Orig.Extension", ".mkv");
                variables.Add("file.Orig.FileName", "OriginalFile");
                variables.Add("file.Orig.FullName", "/media/files/filename.ext");
                variables.Add("file.Orig.Size", 1000);

                variables.Add("file.Create", DateTime.Now);
                variables.Add("file.Create.Day", DateTime.Now.Day);
                variables.Add("file.Create.Month", DateTime.Now.Month);
                variables.Add("file.Create.Year", DateTime.Now.Year);
                variables.Add("file.Modified", DateTime.Now);
                variables.Add("file.Modified.Day", DateTime.Now.Day);
                variables.Add("file.Modified.Month", DateTime.Now.Month);
                variables.Add("file.Modified.Year", DateTime.Now.Year);

                variables.Add("folder.Name", "FolderName");
                variables.Add("folder.FullName", windows ? @"C:\Folder\SubFolder" : "/folder/subfolder");
                variables.Add("folder.Orig.Name", "FolderOriginalName");
                variables.Add("folder.Orig.FullName", windows ? @"C:\OriginalFolder\SubFolder" : "/originalFolder/subfolder");
            }

            PluginHelper pluginHelper = new PluginHelper();

            if (isNew)
            {
                // we add all variables on new, so they can hook up a connection easily
                foreach (var p in flowParts ?? new List<FlowPart>())
                {
                    var partVariables = pluginHelper.GetPartVariables(p.FlowElementUid);
                    foreach (var pv in partVariables)
                    {
                        if (variables.ContainsKey(pv.Key) == false)
                            variables.Add(pv.Key, pv.Value);
                    }
                }
                return variables;
            }

            // get the connected nodes to this part
            var part = flowParts?.Where(x => x.Uid == partUid)?.FirstOrDefault();
            if (part == null)
                return variables;

            List<FlowPart> checkedParts = new List<FlowPart>();

            var parentParts = FindParts(part);
            if (parentParts.Any() == false)
                return variables;

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


                foreach (var p in flowParts ?? new List<FlowPart>())
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

        private FileInfo[] GetTemplateFiles() => new System.IO.DirectoryInfo("Templates/FlowTemplates").GetFiles("*.json");
    
        [HttpGet("templates")]
        public Dictionary<string, List<FlowTemplateModel>> GetTemplates()
        {
            var parts = GetElements().ToDictionary(x => x.Uid.Substring(x.Uid.LastIndexOf(".") + 1), x => x);

            Dictionary<string, List<FlowTemplateModel>> templates = new ();
            string group = string.Empty;
            templates.Add(group, new List<FlowTemplateModel>());
            foreach (var tf in GetTemplateFiles())
            {
                try
                {
                    string json = System.IO.File.ReadAllText(tf.FullName);
                    json = TemplateHelper.ReplaceWindowsPathIfWindows(json);
                    var jsTemplates = System.Text.Json.JsonSerializer.Deserialize<FlowTemplate[]>(json, new System.Text.Json.JsonSerializerOptions
                    {
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true
                    }) ?? new FlowTemplate[] { };
                    foreach (var _jst in jsTemplates)
                    {
                        try
                        {
                            var jstJson = System.Text.Json.JsonSerializer.Serialize(_jst);
                            List<TemplateField> fields = _jst.Fields ?? new List<TemplateField>();
                            // replace all the guids with unique guides
                            for(int i = 1; i < 50; i++)
                            {
                                Guid oldUid = new Guid("00000000-0000-0000-0000-0000000000" + (i < 10 ? "0" : "") + i);
                                Guid newUid = Guid.NewGuid();
                                foreach(var field in fields)
                                {
                                    if (field.Uid == oldUid)
                                        field.Uid = newUid;
                                }
                                jstJson = jstJson.Replace(oldUid.ToString(), newUid.ToString());
                            }
                            var jst = System.Text.Json.JsonSerializer.Deserialize<FlowTemplate>(jstJson);

                            List<FlowPart> flowParts = new List<FlowPart>();
                            int y = DEFAULT_YPOS;
                            foreach (var jsPart in jst.Parts)
                            {
                                var element = parts[jsPart.Node];

                                flowParts.Add(new FlowPart
                                {
                                    yPos = jsPart.yPos ?? y,
                                    xPos = jsPart.xPos ?? DEFAULT_XPOS,
                                    FlowElementUid = element.Uid,
                                    Outputs = jsPart.Outputs ?? element.Outputs,
                                    Inputs = element.Inputs,
                                    Type = element.Type,
                                    Name = jsPart.Name ?? string.Empty,
                                    Uid = jsPart.Uid,
                                    Icon = element.Icon,
                                    Model = jsPart.Model,
                                    OutputConnections = jsPart.Connections?.Select(x => new FlowConnection
                                    {
                                        Input = x.Input,
                                        Output = x.Output,
                                        InputNode = x.Node
                                    }).ToList() ?? new List<FlowConnection>()
                                });
                                y += 150;
                            }

                            if (templates.ContainsKey(_jst.Group ?? String.Empty) == false)
                                templates.Add(_jst.Group ?? String.Empty, new List<FlowTemplateModel>());

                            templates[_jst.Group ?? String.Empty].Add(new FlowTemplateModel
                            {
                                Fields = fields,
                                Order = _jst.Order,
                                Save = jst.Save,
                                Flow = new Flow
                                {
                                    Name = jst.Name,
                                    Template = jst.Name,
                                    Enabled = true,
                                    Description = jst.Description,
                                    Parts = flowParts
                                }
                            });
                        }
                        catch(Exception ex)
                        {
                            Logger.Instance.ELog("Template: " + _jst.Name);
                            Logger.Instance.ELog("Error reading template: " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                } 
                catch (Exception ex)
                {
                    Logger.Instance.ELog("Error reading template: " + ex.Message + Environment.NewLine + ex.StackTrace); 
                }
            }
            foreach(var gn in templates.Keys.ToArray())
            {
                templates[gn] = templates[gn].OrderBy(x =>
                {
                    if (x.Order == null)
                        return int.MaxValue;
                    return x.Order;
                }).ThenBy(x => x.Flow.Name).ToList();
            }
            return templates;
        }
    }

}