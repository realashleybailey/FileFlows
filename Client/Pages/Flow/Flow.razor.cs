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

    public partial class Flow : ComponentBase
    {
        [Parameter] public System.Guid Uid { get; set; }
        [CascadingParameter] Blocker Blocker { get; set; }
        private ffElement[] Available { get; set; }
        private List<ffPart> Parts { get; set; } = new List<ffPart>();

        private FlowPartEditor Editor { get; set; }

        public ffPart SelectedPart { get; set; }
        [Inject]
        private IJSRuntime jsRuntime { get; set; }
        private bool IsSaving { get; set; }
        private bool IsExecuting { get; set; }

        private ff Model { get; set; }

        const string API_URL = "/api/flow";

        private string lblSave, lblSaving, lblExecute, lblExecuting;
        protected override void OnInitialized()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblExecute = Translater.Instant("Labels.Execute");
            lblExecuting = Translater.Instant("Labels.Executing");
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

                var modelResult = await HttpHelper.Get<ff>(API_URL + "/one");
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

        private bool _needsRendering = false;

        private async Task WaitForRender()
        {
            _needsRendering = true;
            StateHasChanged();
            while (_needsRendering)
            {
                await Task.Delay(50);
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            _needsRendering = false;
        }

        private async Task InitModel(ff model)
        {
            this.Model = model;
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
            part.Model = element.Model;
            Logger.Instance.DLog("part.Model: " + part.Model?.GetType()?.Name);
            Parts.Add(part);
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
                        Name = "SOme flow",
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
                    Model = result.Data;
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
        }



        private async Task Execute()
        {
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {
                var result = await HttpHelper.Post<string>(API_URL + "/execute?input=somefile.mp4");
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
        }

        async Task Edit(ffPart part)
        {
            var flowElement = this.Available.FirstOrDefault(x => x.Uid == part.FlowElementUid);
            if (flowElement == null)
            {
                // cant find it, cant edit
                Logger.Instance.DLog("Failed to locate flow element: " + part.FlowElementUid);
                return;
            }
            Logger.Instance.DLog("opening editor for: ", part);
            var newModelTask = Editor.Open(part, flowElement);
            await newModelTask;
            if (newModelTask.IsCanceled == false)
            {
                Logger.Instance.DLog("model updated:" + System.Text.Json.JsonSerializer.Serialize(newModelTask.Result));
                part.Model = newModelTask.Result;
            }
            else
            {
                Logger.Instance.DLog("model canceled");
            }
        }
    }
};