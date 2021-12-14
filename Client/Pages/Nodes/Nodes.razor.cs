namespace FileFlows.Client.Pages
{
    using Radzen;
    using FileFlows.Client.Components;
    using FileFlows.Client.Components.Inputs;

    public partial class Nodes : ListPage<ProcessingNode>
    {
        public override string ApiUrl => "/api/node";
        const string FileFlowsServer = "FileFlowsServer";

        private ProcessingNode EditingItem = null;

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


        public override async Task<bool> Edit(ProcessingNode node)
        {
#if (!DEMO)
            bool isServerNode = node.Address == FileFlowsServer;
            node.Mappings ??= new();
            this.EditingItem = node;
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = Plugin.FormInputType.Text,
                Name = nameof(node.Name),
                Parameters = new ()
                {
                    { nameof(InputText.ReadOnly), isServerNode }
                },
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            if (isServerNode == false)
            {
                fields.Add(new ElementField
                {
                    InputType = Plugin.FormInputType.Text,
                    Name = nameof(node.Address),
                    Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
                });
            }
            fields.Add(new ElementField
            {
                InputType = Plugin.FormInputType.Switch,
                Name = nameof(node.Enabled)
            });
            fields.Add(new ElementField
            {
                InputType = Plugin.FormInputType.Int,
                Name = nameof(node.FlowRunners),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Range() { Minimum = 0, Maximum = 100 } // 100 is insane but meh, let them be insane 
                }
            });
            fields.Add(new ElementField
            {
                InputType = isServerNode ? Plugin.FormInputType.Folder : Plugin.FormInputType.Text,
                Name = nameof(node.TempPath),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            if (isServerNode == false)
            {
                fields.Add(new ElementField
                {
                    InputType = Plugin.FormInputType.KeyValue,
                    Name = nameof(node.Mappings)
                });
            }

            var result = await Editor.Open("Pages.ProcessingNode", "Pages.ProcessingNode.Title", fields, node,large: true,
              saveCallback: Save);
#endif
            return false;
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
                    NotificationService.Notify(NotificationSeverity.Error, saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
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