namespace FileFlows.Client.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Components;
    using FileFlows.Client.Helpers;
    using ffPart = FileFlows.Shared.Models.FlowPart;
    using ffElement = FileFlows.Shared.Models.FlowElement;
    using ff = FileFlows.Shared.Models.Flow;
    using xFlowConnection = FileFlows.Shared.Models.FlowConnection;
    using Microsoft.JSInterop;
    using System.Linq;
    using System;
    using FileFlows.Shared;
    using FileFlows.Client.Components.Dialogs;
    using FileFlows.Shared.Helpers;
    using System.Dynamic;
    using FileFlows.Shared.Models;
    using System.Text.Json;
    using FileFlows.Plugin;

    public partial class Flow : ComponentBase
    {
        [CascadingParameter] public Editor Editor { get; set; }
        [Parameter] public System.Guid Uid { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [CascadingParameter] Blocker Blocker { get; set; }
        private ffElement[] Available { get; set; }
        private List<ffPart> Parts { get; set; } = new List<ffPart>();

        public ffPart SelectedPart { get; set; }
        [Inject]
        private IJSRuntime jsRuntime { get; set; }
        private bool IsSaving { get; set; }
        private bool IsExecuting { get; set; }

        private ff Model { get; set; }

        private string Title { get; set; }

        private string Name { get; set; }

        const string API_URL = "/api/flow";

        private string lblName, lblSave, lblSaving, lblClose;

        private bool _needsRendering = false;

        private bool IsDirty = false;

        protected override void OnInitialized()
        {
            lblName = Translater.Instant("Labels.Name");
            lblSave = Translater.Instant("Labels.Save");
            lblClose = Translater.Instant("Labels.Close");
            lblSaving = Translater.Instant("Labels.Saving");
            _ = Init();
        }

        private async Task Init()
        {
            this.Blocker.Show();
            try
            {
                var elementsResult = await GetElements(API_URL + "/elements");
                if (elementsResult.Success)
                {
                    Available = elementsResult.Data;
                }
                FileFlows.Shared.Models.Flow flow;
                if (Uid == Guid.Empty && App.Instance.NewFlowTemplate != null)
                {
                    flow = App.Instance.NewFlowTemplate;
                    App.Instance.NewFlowTemplate = null;
                }
                else
                {
                    var modelResult = await GetModel(API_URL + "/" + Uid.ToString());
                    flow = (modelResult.Success ? modelResult.Data : null) ?? new ff() { Parts = new List<ffPart>() };
                }
                await InitModel(flow);

                var dotNetObjRef = DotNetObjectReference.Create(this);
                await jsRuntime.InvokeVoidAsync("ffFlow.init", new object[] { "flow-parts", dotNetObjRef, this.Parts });

                await WaitForRender();
                await jsRuntime.InvokeVoidAsync("ffFlow.redrawLines");

            }
            finally
            {
                this.Blocker.Hide();
            }
        }

        private async Task<RequestResult<ff>> GetModel(string url)
        {
#if (DEMO)
            string json = "{\"Enabled\":true,\"Parts\":[{\"Uid\":\"10c99731-370d-41b6-b400-08d003e6e843\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.VideoNodes.VideoFile\",\"xPos\":411,\"yPos\":18,\"Icon\":\"fas fa-video\",\"Inputs\":0,\"Outputs\":1,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"38e28c04-4ce7-4bcf-90f3-79ed0796f347\"}],\"Type\":0,\"Model\":{}},{\"Uid\":\"3121dcae-bfb8-4c37-8871-27618b29beb4\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.VideoNodes.Video_H265_AC3\",\"xPos\":403,\"yPos\":310,\"Icon\":\"far fa-file-video\",\"Inputs\":1,\"Outputs\":2,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"7363e1d1-2cc3-444c-b970-a508e7ef3d42\"},{\"Input\":1,\"Output\":2,\"InputNode\":\"7363e1d1-2cc3-444c-b970-a508e7ef3d42\"}],\"Type\":2,\"Model\":{\"Language\":\"eng\",\"Crf\":21,\"NvidiaEncoding\":true,\"Threads\":0,\"Name\":\"\",\"NormalizeAudio\":false}},{\"Uid\":\"7363e1d1-2cc3-444c-b970-a508e7ef3d42\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.BasicNodes.File.MoveFile\",\"xPos\":404,\"yPos\":489,\"Icon\":\"fas fa-file-export\",\"Inputs\":1,\"Outputs\":1,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"bc8f30c0-a72e-47a4-94fc-7543206705b9\"}],\"Type\":2,\"Model\":{\"DestinationPath\":\"/media/downloads/converted/tv\",\"MoveFolder\":true,\"DeleteOriginal\":true}},{\"Uid\":\"38e28c04-4ce7-4bcf-90f3-79ed0796f347\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.VideoNodes.DetectBlackBars\",\"xPos\":411,\"yPos\":144,\"Icon\":\"fas fa-film\",\"Inputs\":1,\"Outputs\":2,\"OutputConnections\":[{\"Input\":1,\"Output\":1,\"InputNode\":\"3121dcae-bfb8-4c37-8871-27618b29beb4\"},{\"Input\":1,\"Output\":2,\"InputNode\":\"3121dcae-bfb8-4c37-8871-27618b29beb4\"}],\"Type\":3,\"Model\":{}},{\"Uid\":\"bc8f30c0-a72e-47a4-94fc-7543206705b9\",\"Name\":\"\",\"FlowElementUid\":\"FileFlows.BasicNodes.File.DeleteSourceDirectory\",\"xPos\":404,\"yPos\":638,\"Icon\":\"far fa-trash-alt\",\"Inputs\":1,\"Outputs\":2,\"OutputConnections\":null,\"Type\":2,\"Model\":{\"IfEmpty\":true,\"IncludePatterns\":[\"mkv\",\"mp4\",\"divx\",\"avi\"]}}]}";

            var result = System.Text.Json.JsonSerializer.Deserialize<ff>(json);
            return new RequestResult<ff> { Success = true, Data = result };
#else
            return await HttpHelper.Get<ff>(url);
#endif
        }

        private async Task<RequestResult<ffElement[]>> GetElements(string url)
        {
#if (DEMO)
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FileFlows.Shared.Json.ValidatorConverter() }
            };
            string json = "[{\"uid\":\"FileFlows.BasicNodes.File.InputFile\",\"name\":\"InputFile\",\"icon\":\"far fa-file\",\"inputs\":0,\"outputs\":1,\"type\":0,\"group\":\"File\",\"fields\":[],\"model\":{}},{\"uid\":\"FileFlows.BasicNodes.File.CopyFile\",\"name\":\"CopyFile\",\"icon\":\"far fa-copy\",\"inputs\":1,\"outputs\":1,\"type\":2,\"group\":\"File\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"DestinationPath\",\"placeholder\":null,\"inputType\":10,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]},{\"order\":2,\"type\":\"System.Boolean\",\"name\":\"CopyFolder\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"DestinationPath\":null,\"CopyFolder\":false}},{\"uid\":\"FileFlows.BasicNodes.File.DeleteSourceDirectory\",\"name\":\"DeleteSourceDirectory\",\"icon\":\"far fa-trash-alt\",\"inputs\":1,\"outputs\":2,\"type\":2,\"group\":\"File\",\"fields\":[{\"order\":1,\"type\":\"System.Boolean\",\"name\":\"IfEmpty\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":2,\"type\":\"System.String[]\",\"name\":\"IncludePatterns\",\"placeholder\":null,\"inputType\":8,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"IfEmpty\":false,\"IncludePatterns\":null}},{\"uid\":\"FileFlows.BasicNodes.File.MoveFile\",\"name\":\"MoveFile\",\"icon\":\"fas fa-file-export\",\"inputs\":1,\"outputs\":1,\"type\":2,\"group\":\"File\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"DestinationPath\",\"placeholder\":null,\"inputType\":10,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]},{\"order\":2,\"type\":\"System.Boolean\",\"name\":\"MoveFolder\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":3,\"type\":\"System.Boolean\",\"name\":\"DeleteOriginal\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"DestinationPath\":null,\"MoveFolder\":false,\"DeleteOriginal\":false}},{\"uid\":\"FileFlows.BasicNodes.File.Renamer\",\"name\":\"Renamer\",\"icon\":\"fas fa-font\",\"inputs\":1,\"outputs\":1,\"type\":2,\"group\":\"File\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"Pattern\",\"placeholder\":null,\"inputType\":13,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]},{\"order\":2,\"type\":\"System.String\",\"name\":\"DestinationPath\",\"placeholder\":null,\"inputType\":10,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":3,\"type\":\"System.Boolean\",\"name\":\"LogOnly\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":4,\"type\":\"System.String\",\"name\":\"CsvFile\",\"placeholder\":null,\"inputType\":9,\"variables\":null,\"parameters\":{\"Extensions\":[]},\"validators\":[]}],\"model\":{\"Pattern\":null,\"DestinationPath\":null,\"LogOnly\":false,\"CsvFile\":null}},{\"uid\":\"FileFlows.BasicNodes.File.FileExtension\",\"name\":\"FileExtension\",\"icon\":\"far fa-file-excel\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"File\",\"fields\":[{\"order\":1,\"type\":\"System.String[]\",\"name\":\"Extensions\",\"placeholder\":null,\"inputType\":8,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]}],\"model\":{\"Extensions\":null}},{\"uid\":\"FileFlows.BasicNodes.File.FileSize\",\"name\":\"FileSize\",\"icon\":\"fas fa-balance-scale-right\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"File\",\"fields\":[{\"order\":1,\"type\":\"System.Int32\",\"name\":\"Lower\",\"placeholder\":null,\"inputType\":6,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":2,\"type\":\"System.Int32\",\"name\":\"Upper\",\"placeholder\":null,\"inputType\":6,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"Lower\":0,\"Upper\":0}},{\"uid\":\"FileFlows.BasicNodes.Functions.Function\",\"name\":\"Function\",\"icon\":\"fas fa-code\",\"inputs\":1,\"outputs\":0,\"type\":3,\"group\":\"Functions\",\"fields\":[{\"order\":1,\"type\":\"System.Int32\",\"name\":\"Outputs\",\"placeholder\":null,\"inputType\":6,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":2,\"type\":\"System.String\",\"name\":\"Code\",\"placeholder\":null,\"inputType\":5,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]}],\"model\":{\"Outputs\":1,\"Code\":\"// Variables contain variables available to this node from previous nodes.\\n// Logger lets you log messages to the flow output.\\n\\n// return 0 to complete the flow.\\n// return -1 to signal an error in the flow\\n// return 1+ to select which output node will be processed next\\n\\nif(Variables.FileSize === 0)\\n\\treturn -1;\\n\\nreturn 0;\"}},{\"uid\":\"FileFlows.BasicNodes.Functions.Log\",\"name\":\"Log\",\"icon\":\"far fa-file-alt\",\"inputs\":1,\"outputs\":1,\"type\":3,\"group\":\"Functions\",\"fields\":[{\"order\":1,\"type\":\"FileFlows.Plugin.LogType\",\"name\":\"LogType\",\"placeholder\":null,\"inputType\":3,\"variables\":null,\"parameters\":{\"Options\":[{\"label\":\"Enums.LogType.Info\",\"value\":2},{\"label\":\"Enums.LogType.Debug\",\"value\":3},{\"label\":\"Enums.LogType.Warning\",\"value\":1},{\"label\":\"Enums.LogType.Error\",\"value\":0}]},\"validators\":[]},{\"order\":2,\"type\":\"System.String\",\"name\":\"Message\",\"placeholder\":null,\"inputType\":4,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]}],\"model\":{\"LogType\":0,\"Message\":null}},{\"uid\":\"FileFlows.BasicNodes.Functions.PatternMatch\",\"name\":\"PatternMatch\",\"icon\":\"fas fa-equals\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"Functions\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"Pattern\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]}],\"model\":{\"Pattern\":\"\"}},{\"uid\":\"FileFlows.BasicNodes.Functions.PatternReplacer\",\"name\":\"PatternReplacer\",\"icon\":\"fas fa-exchange-alt\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"Functions\",\"fields\":[{\"order\":1,\"type\":\"System.Collections.Generic.List`1[[System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]\",\"name\":\"Replacements\",\"placeholder\":null,\"inputType\":14,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]},{\"order\":2,\"type\":\"System.Boolean\",\"name\":\"UseWorkingFileName\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"Replacements\":null,\"UseWorkingFileName\":false}},{\"uid\":\"MetaNodes.TheMovieDb.MovieLookup\",\"name\":\"MovieLookup\",\"icon\":\"fas fa-film\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"TheMovieDb\",\"fields\":[{\"order\":1,\"type\":\"System.Boolean\",\"name\":\"UseFolderName\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"UseFolderName\":false}},{\"uid\":\"FileFlows.VideoNodes.VideoFile\",\"name\":\"VideoFile\",\"icon\":\"fas fa-video\",\"inputs\":0,\"outputs\":1,\"type\":0,\"group\":\"VideoNodes\",\"fields\":[],\"model\":{}},{\"uid\":\"FileFlows.VideoNodes.FFMPEG\",\"name\":\"FFMPEG\",\"icon\":\"far fa-file-video\",\"inputs\":1,\"outputs\":1,\"type\":2,\"group\":\"VideoNodes\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"CommandLine\",\"placeholder\":null,\"inputType\":4,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]},{\"order\":2,\"type\":\"System.String\",\"name\":\"Extension\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]}],\"model\":{\"CommandLine\":\"-i {WorkingFile} {TempDir}output.mkv\",\"Extension\":\"mkv\"}},{\"uid\":\"FileFlows.VideoNodes.VideoEncode\",\"name\":\"VideoEncode\",\"icon\":\"far fa-file-video\",\"inputs\":1,\"outputs\":2,\"type\":2,\"group\":\"VideoNodes\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"VideoCodec\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":2,\"type\":\"System.String\",\"name\":\"VideoCodecParameters\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":3,\"type\":\"System.String\",\"name\":\"AudioCodec\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":4,\"type\":\"System.String\",\"name\":\"Language\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":5,\"type\":\"System.String\",\"name\":\"Extension\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"VideoCodec\":\"hevc\",\"VideoCodecParameters\":\"hevc_nvenc -preset hq -crf 23\",\"AudioCodec\":\"ac3\",\"Language\":\"eng\",\"Extension\":\"mkv\"}},{\"uid\":\"FileFlows.VideoNodes.Video_H265_AC3\",\"name\":\"Video_H265_AC3\",\"icon\":\"far fa-file-video\",\"inputs\":1,\"outputs\":2,\"type\":2,\"group\":\"VideoNodes\",\"fields\":[{\"order\":1,\"type\":\"System.String\",\"name\":\"Language\",\"placeholder\":null,\"inputType\":1,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":2,\"type\":\"System.Int32\",\"name\":\"Crf\",\"placeholder\":null,\"inputType\":6,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":3,\"type\":\"System.Boolean\",\"name\":\"NvidiaEncoding\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":4,\"type\":\"System.Int32\",\"name\":\"Threads\",\"placeholder\":null,\"inputType\":6,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":5,\"type\":\"System.Boolean\",\"name\":\"NormalizeAudio\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]},{\"order\":6,\"type\":\"System.Boolean\",\"name\":\"ForceRencode\",\"placeholder\":null,\"inputType\":2,\"variables\":null,\"parameters\":{},\"validators\":[]}],\"model\":{\"Language\":\"eng\",\"Crf\":21,\"NvidiaEncoding\":true,\"Threads\":0,\"NormalizeAudio\":false,\"ForceRencode\":false}},{\"uid\":\"FileFlows.VideoNodes.DetectBlackBars\",\"name\":\"DetectBlackBars\",\"icon\":\"fas fa-film\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"VideoNodes\",\"fields\":[],\"model\":{}},{\"uid\":\"FileFlows.VideoNodes.VideoCodec\",\"name\":\"VideoCodec\",\"icon\":\"fas fa-video\",\"inputs\":1,\"outputs\":2,\"type\":3,\"group\":\"VideoNodes\",\"fields\":[{\"order\":1,\"type\":\"System.String[]\",\"name\":\"Codecs\",\"placeholder\":null,\"inputType\":8,\"variables\":null,\"parameters\":{},\"validators\":[{\"type\":\"Required\"}]}],\"model\":{\"Codecs\":null}}]";
            var elements = JsonSerializer.Deserialize<ffElement[]>(json, options);
            return new RequestResult<ffElement[]> { Success = true, Data = elements };
#else
            return await HttpHelper.Get<ffElement[]>(url);
#endif
        }

        private async Task WaitForRender()
        {
            _needsRendering = true;
            StateHasChanged();
            while (_needsRendering)
            {
                await Task.Delay(50);
            }
        }

        async Task Close()
        {
            if (IsDirty)
            {
                bool result = await Confirm.Show(lblClose, $"Pages.{nameof(Flow)}.Messages.Close");
                if (result == false)
                    return;
            }
            NavigationManager.NavigateTo("flows");
        }


        protected override void OnAfterRender(bool firstRender)
        {
            _needsRendering = false;
        }

        private void SetTitle()
        {
            this.Title = Translater.Instant("Pages.Flow.Title", Model) + ":";
        }

        private async Task InitModel(ff model)
        {
            this.Model = model;
            this.SetTitle();
            this.Model.Parts ??= new List<ffPart>(); // just incase its null
            this.Parts = this.Model.Parts;

            this.Name = model.Name ?? "";

            var connections = new Dictionary<string, List<xFlowConnection>>();
            foreach (var part in this.Parts.Where(x => x.OutputConnections?.Any() == true))
            {
                connections.Add(part.Uid.ToString(), part.OutputConnections);
            }
            await jsRuntime.InvokeVoidAsync("ffFlow.ioInitConnections", connections);

        }

        [JSInvokable]
        public object AddElement(string uid)
        {
            var element = this.Available.FirstOrDefault(x => x.Uid == uid);
            return new { element, uid = Guid.NewGuid() };
        }
        private async Task Save()
        {
#if (DEMO)
            return;
#else
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {

                var parts = await jsRuntime.InvokeAsync<List<FileFlows.Shared.Models.FlowPart>>("ffFlow.getModel");

                Model ??= new ff();
                Model.Name = this.Name;
                // ensure there are no duplicates and no rogue connections
                Guid[] nodeUids = parts.Select(x => x.Uid).ToArray();
                foreach (var p in parts)
                {
                    p.OutputConnections = p.OutputConnections
                                          ?.Where(x => nodeUids.Contains(x.InputNode))
                                          ?.GroupBy(x => x.Output).Select(x => x.First())
                                          ?.ToList();
                }
                Model.Parts = parts;
                var result = await HttpHelper.Put<ff>(API_URL, Model);
                if (result.Success)
                {
                    Model = result.Data;
                    IsDirty = false;
                }
                else
                {
                    Toast.ShowError(
                        result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant($"ErrorMessages.UnexpectedError") : Translater.TranslateIfNeeded(result.Body),
                        duration: 60_000
                    );
                }
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
#endif
        }

        private async Task<RequestResult<Dictionary<string, object>>> GetVariables(string url, List<FileFlows.Shared.Models.FlowPart> parts)
        {
#if (DEMO)
            string json = "{\"ext\":\".mkv\",\"fileName\":\"Filename\",\"fileSize\":1000,\"fileOrigExt\":\".mkv\",\"fileOrigFileName\":\"OriginalFile\",\"folderName\":\"FolderName\",\"folderFullName\":\"/folder/subfolder\",\"folderOrigName\":\"FolderOriginalName\",\"folderOrigFullName\":\"/originalFolder/subfolder\",\"viVideoCodec\":\"hevc\",\"viAudioCodec\":\"ac3\",\"viAudioCodecs\":\"ac3,aac\",\"viAudioLanguage\":\"eng\",\"viAudioLanguages\":\"eng, mao\",\"viResolution\":\"1080p\"}";
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return new RequestResult<Dictionary<string, object>> { Success = true, Data = dict };
#else
            return await HttpHelper.Post<Dictionary<string, object>>(url, parts);
#endif
        }


        [JSInvokable]
        public async Task<object> Edit(ffPart part, bool isNew = false)
        {
            // get flow variables that we can pass onto the form
            Blocker.Show();
            Dictionary<string, object> variables = new Dictionary<string, object>();
            try
            {
                var parts = await jsRuntime.InvokeAsync<List<FlowPart>>("ffFlow.getModel");
                Logger.Instance.DLog("Parts", parts);
                var variablesResult = await GetVariables(API_URL + "/" + part.Uid + "/variables?isNew=" + isNew, parts);
                if (variablesResult.Success)
                    variables = variablesResult.Data;
            }
            finally { Blocker.Hide(); }


            var flowElement = this.Available.FirstOrDefault(x => x.Uid == part.FlowElementUid);
            if (flowElement == null)
            {
                // cant find it, cant edit
                Logger.Instance.DLog("Failed to locate flow element: " + part.FlowElementUid);
                return null;
            }

            string typeName = part.FlowElementUid.Substring(part.FlowElementUid.LastIndexOf(".") + 1);
            string typeDisplayName = Translater.TranslateIfHasTranslation($"Flow.Parts.{typeName}.Label", FlowHelper.FormatLabel(typeName));

            var fields = ObjectCloner.Clone(flowElement.Fields);
            // add the name to the fields, so a node can be renamed
            fields.Insert(0, new ElementField
            {
                Name = "Name",
                Placeholder = typeDisplayName,
                InputType = Plugin.FormInputType.Text
            });


            List<ListOption> flowOptions = null;
            
            foreach (var field in fields)
            {
                field.Variables = variables;
                // special case, load "Flow" into FLOW_LIST
                // this lets a plugin request the list of flows to be shown
                if (field.Parameters?.Any() == true)
                {
                    if (field.Parameters.ContainsKey("OptionsProperty") && field.Parameters["OptionsProperty"] is JsonElement optProperty)
                    {
                        if (optProperty.ValueKind == JsonValueKind.String)
                        {
                            string optp = optProperty.GetString();
                            Logger.Instance.DLog("OptionsProperty = " + optp);
                            if (optp == "FLOW_LIST")
                            {
                                if(flowOptions == null)
                                {
                                    flowOptions = new List<ListOption>();
                                    var flowsResult = await HttpHelper.Get<ff[]>($"/api/flow");
                                    if (flowsResult.Success)
                                    {
                                        flowOptions = flowsResult.Data?.Where(x => x.Uid != Model?.Uid)?.OrderBy(x => x.Name)?.Select(x => new ListOption
                                        {
                                            Label = x.Name,
                                            Value = new ObjectReference
                                            {
                                                Name = x.Name,
                                                Uid = x.Uid,
                                                Type = x.GetType().FullName
                                            }
                                        })?.ToList() ?? new List<ListOption>();
                                    }

                                }
                                if (field.Parameters.ContainsKey("Options"))
                                    field.Parameters["Options"] = flowOptions;
                                else
                                    field.Parameters.Add("Options", flowOptions);
                            }
                        }
                    }
                }
            }

            var model = part.Model ?? new ExpandoObject();
            // add the name to the model, since this is actually bound on the part not model, but we need this 
            // so the user can update the name
            if (model is IDictionary<string, object> dict)
            {
                dict["Name"] = part.Name ?? string.Empty;
            }



            string title = typeDisplayName;
            var newModelTask = Editor.Open("Flow.Parts." + typeName, title, fields, model, large: fields.Count > 1);
            try
            {
                await newModelTask;
            }
            catch (Exception)
            {
                // can throw if canceled
                return null;
            }
            if (newModelTask.IsCanceled == false)
            {
                IsDirty = true;
                var newModel = newModelTask.Result;
                int outputs = -1;
                if (part.Model is IDictionary<string, object> dictNew)
                {
                    if (dictNew?.ContainsKey("Outputs") == true && int.TryParse(dictNew["Outputs"]?.ToString(), out outputs)) { }
                }
                return new { outputs, model = newModel };
            }
            else
            {
                return null;
            }
        }
    }
}