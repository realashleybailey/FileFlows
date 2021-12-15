using FileFlows.Plugin;

namespace FileFlows.Client.Pages
{
    public partial class Nodes : ListPage<ProcessingNode>
    {

        public override async Task<bool> Edit(ProcessingNode node)
        {
#if (!DEMO)

            bool isServerProcessingNode = node.Address == FileFlowsServer;
            node.Mappings ??= new();
            this.EditingItem = node;


            var tabs = new Dictionary<string, List<ElementField>>();
            tabs.Add("General", TabGeneral(node, isServerProcessingNode));
            tabs.Add("Schedule", TabSchedule(node, isServerProcessingNode));
            if(isServerProcessingNode == false)
                tabs.Add("Advanced", TabMappings(node));


            var result = await Editor.Open("Pages.ProcessingNode", "Pages.ProcessingNode.Title", null, node, tabs: tabs, large: true,
              saveCallback: Save);
#endif
            return false;
        }



        private List<ElementField> TabGeneral(ProcessingNode node, bool isServerProcessingNode)
        {
            List<ElementField> fields = new List<ElementField>();

            if (isServerProcessingNode)
            {
                fields.Add(new ElementField
                {
                    InputType = FormInputType.Label,
                    Name = "InternalProcessingNodeDescription"
                });
            }
            else
            {
                fields.Add(new ElementField
                {
                    InputType = FormInputType.Text,
                    Name = nameof(node.Name),
                    Validators = new List<FileFlows.Shared.Validators.Validator> {
                        new FileFlows.Shared.Validators.Required()
                    }
                });
                fields.Add(new ElementField
                {
                    InputType = FormInputType.Text,
                    Name = nameof(node.Address),
                    Validators = new List<FileFlows.Shared.Validators.Validator> {
                        new FileFlows.Shared.Validators.Required()
                    }
                });
            }

            fields.Add(new ElementField
            {
                InputType = FormInputType.Switch,
                Name = nameof(node.Enabled)
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Int,
                Name = nameof(node.FlowRunners),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Range() { Minimum = 0, Maximum = 100 } // 100 is insane but meh, let them be insane 
                },
                Parameters = new()
                {
                    { "Min", 0 },
                    { "Max", 100 }
                }
            });
            fields.Add(new ElementField
            {
                InputType = isServerProcessingNode ? FormInputType.Folder : FormInputType.Text,
                Name = nameof(node.TempPath),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
            return fields;
        }

        private List<ElementField> TabSchedule(ProcessingNode node, bool isServerProcessingNode)
        {
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FormInputType.Label,
                Name = "ScheduleDescription"
            });

            fields.Add(new ElementField
            {
                InputType = FormInputType.Schedule,
                Name = nameof(node.Schedule),
                Parameters = new Dictionary<string, object>
                {
                    { "HideLabel", true }
                }
            });
            return fields;

        }
        private List<ElementField> TabMappings(ProcessingNode node)
        {
            List<ElementField> fields = new List<ElementField>();
            fields.Add(new ElementField
            {
                InputType = FormInputType.Label,
                Name = "MappingsDescription"
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.KeyValue,
                Name = nameof(node.Mappings),
                Parameters = new()
                {
                    { "HideLabel", true }
                }
            });
            return fields;
        }
    }
}
