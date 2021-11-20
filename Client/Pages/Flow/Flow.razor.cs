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
    using Radzen;
    using FileFlows.Shared.Helpers;
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
                await jsRuntime.InvokeVoidAsync("ffFlow.init", new object[] { "flow-parts", dotNetObjRef, this.Parts });

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

        [JSInvokable]
        public object AddElement(string uid)
        {
            Logger.Instance.DLog("looking up element: " + uid);
            var element = this.Available.FirstOrDefault(x => x.Uid == uid);
            return new { element, uid = Guid.NewGuid() };
        }

        private void Select(ffPart part)
        {
            SelectedPart = part;
        }

        private async Task Save()
        {
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {

                var parts = await jsRuntime.InvokeAsync<List<FileFlows.Shared.Models.FlowPart>>("ffFlow.getModel");
                Logger.Instance.DLog("Parts", parts);

                if (Model == null)
                {
                    Model = new ff
                    {
                        Name = "New flow",
                    };
                }
                // ensure there are no duplicates and no rogue connections
                Guid[] nodeUids = parts.Select(x => x.Uid).ToArray();
                foreach (var p in parts)
                {
                    p.OutputConnections = p.OutputConnections
                                          ?.Where(x => nodeUids.Contains(x.InputNode))
                                          ?.GroupBy(x => x.Input + "," + x.Output + "," + x.InputNode).Select(x => x.First())
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


        [JSInvokable]
        public async Task<object> Edit(ffPart part)
        {
            var flowElement = this.Available.FirstOrDefault(x => x.Uid == part.FlowElementUid);
            if (flowElement == null)
            {
                // cant find it, cant edit
                Logger.Instance.DLog("Failed to locate flow element: " + part.FlowElementUid);
                return null;
            }
            string title = Helpers.FlowHelper.FormatLabel(part.Name);
            var newModelTask = Editor.Open("Flow.Parts." + part.Name, title, ObjectCloner.Clone(flowElement.Fields), part.Model ?? new ExpandoObject());
            await newModelTask;
            if (newModelTask.IsCanceled == false)
            {
                IsDirty = true;
                Logger.Instance.DLog("model updated:" + System.Text.Json.JsonSerializer.Serialize(newModelTask.Result));
                var model = newModelTask.Result;
                int outputs = -1;
                if (part.Model is IDictionary<string, object> dict)
                {
                    if (dict?.ContainsKey("Outputs") == true && int.TryParse(dict["Outputs"]?.ToString(), out outputs)) { }
                }
                return new { outputs, model };
            }
            else
            {
                Logger.Instance.DLog("model canceled");
                return null;
            }
        }
    }
}