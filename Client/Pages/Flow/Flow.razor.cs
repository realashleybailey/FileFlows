namespace FileFlow.Client.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlow.Client.Components;
    using FileFlow.Client.Helpers;
    using ffPart = FileFlow.Shared.Models.FlowPart;
    using ffElement = FileFlow.Shared.Models.FlowElement;
    using ff = FileFlow.Shared.Models.Flow;
    using xFlowConnection = FileFlow.Shared.Models.FlowConnection;
    using Microsoft.JSInterop;
    using System.Linq;
    using System;
    using FileFlow.Shared;
    using FileFlow.Client.Components.Dialogs;
    using Radzen;
    using FileFlow.Shared.Helpers;
    using System.Dynamic;

    public partial class Flow : ComponentBase
    {
        [CascadingParameter] public Editor Editor { get; set; }
        [Parameter] public System.Guid Uid { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] public NotificationService NotificationService { get; set; }
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

        const string API_URL = "/api/flow";

        private string lblSave, lblSaving, lblClose, lblRename;

        private bool _needsRendering = false;

        private bool IsDirty = false;

        protected override void OnInitialized()
        {
            lblRename = Translater.Instant("Labels.Rename");
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
                var elementsResult = await HttpHelper.Get<ffElement[]>(API_URL + "/elements");
                if (elementsResult.Success)
                {
                    Available = elementsResult.Data;
                    // foreach(var item in Available){
                    //     if(item.Model is JObject jObject){
                    //         item.Model = jObject.ToObject<Dictionary<string, object>>();
                    //     }
                    // }
                }
                var modelResult = await HttpHelper.Get<ff>(API_URL + "/" + Uid.ToString());
                await InitModel((modelResult.Success ? modelResult.Data : null) ?? new ff() { Parts = new List<ffPart>() });

                var dotNetObjRef = DotNetObjectReference.Create(this);
                await jsRuntime.InvokeVoidAsync("ffFlow.init", new object[] { "flow-parts", dotNetObjRef });

                await WaitForRender();
                await jsRuntime.InvokeVoidAsync("ffFlow.redrawLines");

            }
            finally
            {
                this.Blocker.Hide();
            }
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

        async Task Rename()
        {
            string message = Translater.Instant("Pages.Flow.Labels.Rename");
            string newName = await Prompt.Show(message, "", Model.Name);
            if (string.IsNullOrEmpty(newName))
                return; // was canceled

            Blocker.Show();
            try
            {
                var result = await HttpHelper.Put(API_URL + "/" + Model.Uid + "/rename?name=" + Uri.EscapeDataString(newName));

                if (result.Success == false)
                {
                    NotificationService.Notify(NotificationSeverity.Error,
                        Translater.TranslateIfNeeded(result.Body?.EmptyAsNull() ?? "ErrorMessages.UnexpectedError")
                    );
                    return;
                }
                Model.Name = newName;
                this.SetTitle();
                this.StateHasChanged();
            }
            finally
            {
                Blocker.Hide();
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            _needsRendering = false;
        }

        private void SetTitle()
        {
            this.Title = Translater.Instant("Pages.Flow.Labels.EditFlow", Model);
        }

        private async Task InitModel(ff model)
        {
            this.Model = model;
            this.SetTitle();
            this.Model.Parts ??= new List<ffPart>(); // just incase its null
            this.Parts = this.Model.Parts;

            var connections = new Dictionary<string, List<xFlowConnection>>();
            foreach (var part in this.Parts.Where(x => x.OutputConnections?.Any() == true))
            {
                connections.Add(part.Uid.ToString(), part.OutputConnections);
            }
            await jsRuntime.InvokeVoidAsync("ffFlow.ioInitConnections", connections);

        }

        private void Select(ffPart part)
        {
            SelectedPart = part;
        }

        internal async Task DeletePart(ffPart part)
        {
            if (part != null)
            {
                IsDirty = true;
                this.Parts.Remove(part);
                this.StateHasChanged();
                await WaitForRender();
                await InitModel(this.Model);
                await jsRuntime.InvokeVoidAsync("ffFlow.redrawLines");
            }
        }

        [JSInvokable]
        public void UpdatePosition(string uidString, int xPos, int yPos)
        {
            Guid uid;
            if (Guid.TryParse(uidString, out uid) == false)
                return;
            var part = this.Parts.FirstOrDefault(x => x.Uid == uid);
            if (part == null)
                return;

            IsDirty = true;
            Logger.Instance.DLog($"Updating part position {xPos}, {yPos}");
            part.xPos = xPos;
            part.yPos = yPos;
        }

        [JSInvokable]
        public void AddElement(string uid, int xPos, int yPos)
        {
            var element = this.Available.FirstOrDefault(x => x.Uid == uid);
            if (element == null)
                return;
            IsDirty = true;

            Logger.Instance.DLog($"Addeing element {xPos}, {yPos}");
            AddPart(element, xPos, yPos);
            this.StateHasChanged();
        }

        [JSInvokable]
        public void AddConnection(string uidInput, string uidOutput, int input, int output)
        {
            Logger.Instance.DLog($"adding connnection 0: {uidInput}, {uidOutput}, {input}, {output}");
            var fpInput = this.Parts.FirstOrDefault(x => x.Uid.ToString() == uidInput);
            var fpOutput = this.Parts.FirstOrDefault(x => x.Uid.ToString() == uidOutput);
            if (fpInput == null || fpOutput == null)
                return;
            IsDirty = true;
            Logger.Instance.DLog($"adding connnection 1: {uidInput}, {uidOutput}, {input}, {output}");


            fpOutput.OutputConnections ??= new List<FileFlow.Shared.Models.FlowConnection>();
            fpOutput.OutputConnections.Add(new FileFlow.Shared.Models.FlowConnection
            {
                Input = input,
                Output = output,
                InputNode = fpInput.Uid
            });
            // make sure there are no duplicates
            fpOutput.OutputConnections = fpOutput.OutputConnections.GroupBy(x => x.Input + "," + x.Output + "," + x.InputNode).Select(x => x.First()).ToList();
        }

        [JSInvokable]
        public void RemoveConnection(string uidInput, string uidOutput, int input, int output)
        {
            var fpInput = this.Parts.FirstOrDefault(x => x.Uid.ToString() == uidInput);
            var fpOutput = this.Parts.FirstOrDefault(x => x.Uid.ToString() == uidOutput);
            if (fpInput == null || fpOutput == null)
                return;

            IsDirty = true;
            fpOutput.OutputConnections = fpOutput.OutputConnections?.Where(x => x.Input != input && x.Output != output && x.InputNode != fpInput.Uid)?.ToList();
        }
        private void AddPart(ffElement element, float xPos, float yPos)
        {
            var part = new ffPart();
            part.Name = element.Name;
            part.FlowElementUid = element.Uid;
            part.Type = element.Type;
            part.xPos = xPos;
            part.yPos = yPos;
            part.Inputs = element.Inputs;
            part.Outputs = element.Outputs;
            part.Uid = Guid.NewGuid();
            part.Icon = element.Icon;
            // we have to clone the model, not use the same instance
            if (element.Model != null)
                part.Model = FileFlow.Shared.Helpers.ObjectCloner.Clone(element.Model);
            if (part.Model != null && part.Model is IDictionary<string, object> dict)
            {
                if (dict?.ContainsKey("Outputs") == true && int.TryParse(dict["Outputs"]?.ToString() ?? "", out int outputs))
                {
                    part.Outputs = outputs;
                }
            }
            Logger.Instance.DLog("part.Model: " + part.Model?.GetType()?.Name);
            Parts.Add(part);
            IsDirty = true;
        }

        private async Task Save()
        {
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {
                if (Model == null)
                {
                    Model = new ff
                    {
                        Name = "New flow",
                    };
                }
                // ensure there are no duplicates and no rogue connections
                Guid[] nodeUids = Parts.Select(x => x.Uid).ToArray();
                foreach (var p in Parts)
                {
                    p.OutputConnections = p.OutputConnections
                                          ?.Where(x => nodeUids.Contains(x.InputNode))
                                          ?.GroupBy(x => x.Input + "," + x.Output + "," + x.InputNode).Select(x => x.First())
                                          ?.ToList();
                }
                Model.Parts = this.Parts;
                var result = await HttpHelper.Put<ff>(API_URL, Model);
                if (result.Success)
                {
                    Model = result.Data;
                    IsDirty = false;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error,
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
        }


        public async Task<bool> Edit(ffPart part)
        {
            var flowElement = this.Available.FirstOrDefault(x => x.Uid == part.FlowElementUid);
            if (flowElement == null)
            {
                // cant find it, cant edit
                Logger.Instance.DLog("Failed to locate flow element: " + part.FlowElementUid);
                return false;
            }
            string title = Helpers.FlowHelper.FormatLabel(part.Name);
            var newModelTask = Editor.Open("Flow.Parts." + part.Name, title, ObjectCloner.Clone(flowElement.Fields), part.Model ?? new ExpandoObject());
            await newModelTask;
            if (newModelTask.IsCanceled == false)
            {
                IsDirty = true;
                Logger.Instance.DLog("model updated:" + System.Text.Json.JsonSerializer.Serialize(newModelTask.Result));
                part.Model = newModelTask.Result;
                if (part.Model is IDictionary<string, object> dict)
                {
                    if (dict?.ContainsKey("Outputs") == true && int.TryParse(dict["Outputs"]?.ToString(), out int outputs))
                        part.Outputs = outputs;
                }
                return true;
            }
            else
            {
                Logger.Instance.DLog("model canceled");
                return false;
            }
        }
    }
}