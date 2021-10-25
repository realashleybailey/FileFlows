namespace ViWatcher.Client.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using ViWatcher.Client.Components;
    using ViWatcher.Client.Helpers;
    using viFlowPart = ViWatcher.Shared.Models.FlowPart;
    using viFlowElement = ViWatcher.Shared.Models.FlowElement;
    using Microsoft.JSInterop;
    using System.Linq;
    using System;

    public partial class Flow:ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        private viFlowElement[] Available{ get; set; }
        private List<viFlowPart> Parts { get; set; } = new List<viFlowPart>();
        public viFlowPart SelectedPart{ get; set; }
        [Inject]
        private IJSRuntime jsRuntime{ get; set; }
        protected override async Task OnInitializedAsync()
        {
            this.Blocker.Show();
            try
            {
                var elementsResult = await HttpHelper.Get<viFlowElement[]>("/api/flow/elements");
                if (elementsResult.Success)
                {
                    Available = elementsResult.Data;
                    AddPart(Available[0], 100, 100);
                    AddPart(Available[1], 400, 300);
                    AddPart(Available[2], 600, 600);
                }
                var dotNetObjRef = DotNetObjectReference.Create(this);
                await jsRuntime.InvokeVoidAsync("ViFlow.init", new object[] { "flow-parts", dotNetObjRef});

            }finally 
            {
                this.Blocker.Hide();
            }
        }

        private void Select(viFlowPart part)
        {
            SelectedPart = part;
        }

        [JSInvokable]
        public void UpdatePosition(string uidString, int xPos, int yPos)
        {
            Guid uid;
            if(Guid.TryParse(uidString, out uid) == false)
                 return;
            var part = this.Parts.FirstOrDefault(x => x.Uid == uid);
            if(part == null)
                return;

            Logger.Instance.DLog($"Updating part position {xPos}, {yPos}");
            part.xPos = xPos;
            part.yPos = yPos;
        }

        [JSInvokable]
        public void AddElement(string uidString, int xPos, int yPos)
        {
            Guid uid;
            if(Guid.TryParse(uidString, out uid) == false)
                 return;
            var element = this.Available.FirstOrDefault(x => x.Uid == uid);
            if(element == null)
                return;

            Logger.Instance.DLog($"Addeing element {xPos}, {yPos}");
            AddPart(element, xPos, yPos);
            this.StateHasChanged();
        }

        private void AddPart(viFlowElement element, float xPos, float yPos)
        {            
            var part = new viFlowPart();
            part.Name = element.Name;
            part.FlowElementUid = element.Uid;
            part.Type = element.Type;
            part.xPos = xPos;
            part.yPos = yPos;
            part.Inputs = element.Inputs;
            part.Outputs = element.Outputs;
            part.Uid = Guid.NewGuid();
            Parts.Add(part);
        }
    }
}