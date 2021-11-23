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
                var elementsResult = await HttpHelper.Get<ffElement[]>(API_URL + "/elements");
                if (elementsResult.Success)
                {
                    Available = elementsResult.Data;
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
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {

                var parts = await jsRuntime.InvokeAsync<List<FileFlows.Shared.Models.FlowPart>>("ffFlow.getModel");
                Logger.Instance.DLog("Parts", parts);

                Model ??= new ff();
                Model.Name = this.Name;
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
            // get flow variables that we can pass onto the form
            Blocker.Show();
            List<string> variables = new List<string>();
            try
            {
                var parts = await jsRuntime.InvokeAsync<List<FileFlows.Shared.Models.FlowPart>>("ffFlow.getModel");
                Logger.Instance.DLog("Parts: ", parts);
                var variablesResult = await HttpHelper.Post<List<string>>(API_URL +"/" + part.Uid + "/variables", parts);
                if(variablesResult.Success)
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

            var fields = ObjectCloner.Clone(flowElement.Fields);
            // add the name to the fields, so a node can be renamed
            fields.Insert(0, new FileFlows.Shared.Models.ElementField
            {
                Name = "Name",
                Placeholder = FlowHelper.FormatLabel(typeName),
                InputType = Plugin.FormInputType.Text
            });

            foreach(var field in fields)
            {
                if(field.InputType == Plugin.FormInputType.TextVariable)
                    field.Variables = variables;
            }

            var model = part.Model ?? new ExpandoObject();
            // add the name to the model, since this is actually bound on the part not model, but we need this 
            // so the user can update the name
            if (model is IDictionary<string, object> dict)
                dict["Name"] = part.Name ?? string.Empty;

            string title = Helpers.FlowHelper.FormatLabel(typeName);
            var newModelTask = Editor.Open("Flow.Parts." + typeName, title, fields, model, large:fields.Count > 1);
            await newModelTask;
            if (newModelTask.IsCanceled == false)
            {
                IsDirty = true;
                Logger.Instance.DLog("model updated:" + System.Text.Json.JsonSerializer.Serialize(newModelTask.Result));
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
                Logger.Instance.DLog("model canceled");
                return null;
            }
        }
    }
}