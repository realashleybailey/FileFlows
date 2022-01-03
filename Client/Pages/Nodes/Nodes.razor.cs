namespace FileFlows.Client.Pages
{
    using FileFlows.Client.Components;

    public partial class Nodes : ListPage<ProcessingNode>
    {
        public override string ApiUrl => "/api/node";
        const string FileFlowsServer = "FileFlowsServer";

        private ProcessingNode EditingItem = null;

        private string lblAddress, lblRunners;
        protected override void OnInitialized()
        {
            base.OnInitialized();
            lblAddress = Translater.Instant("Pages.Nodes.Labels.Address");
            lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        }


        private async Task Add()
        {
#if (!DEMO)
            await Edit(new ProcessingNode());
#endif
        }

        public override Task PostLoad()
        {
            var serverNode = this.Data?.Where(x => x.Address == FileFlowsServer).FirstOrDefault();
            if(serverNode != null)
            {
                serverNode.Name = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");                
            }
            return base.PostLoad();
        }


        async Task Enable(bool enabled, ProcessingNode node)
        {
#if (DEMO)
            return;
#else
            Blocker.Show();
            try
            {
                await HttpHelper.Put<ProcessingNode>($"{ApiUrl}/state/{node.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
            }
#endif
        }

        async Task<bool> Save(ExpandoObject model)
        {
#if (DEMO)
            return true;
#else
            Blocker.Show();
            this.StateHasChanged();

            try
            {
                var saveResult = await HttpHelper.Post<ProcessingNode>($"{ApiUrl}", model);
                if (saveResult.Success == false)
                {
                    Toast.ShowError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                    return false;
                }

                int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
                if (index < 0)
                    this.Data.Add(saveResult.Data);
                else
                    this.Data[index] = saveResult.Data;
                await this.Load(saveResult.Data.Uid);

                return true;
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
#endif
        }

    }
}