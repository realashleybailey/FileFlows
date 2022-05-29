using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

public partial class Scripts
{

    /// <summary>
    /// Editor for a Script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>the result of the edit</returns>
    public override async Task<bool> Edit(Script item)
    {
        this.EditingItem = item;

        var tabs = new Dictionary<string, List<ElementField>>();
        tabs.Add("General", TabGeneral(item));
        tabs.Add("Outputs", TabOutputs(item));

        var result = await Editor.Open("Pages.Script", "Pages.Script.Title", null, item, tabs: tabs, large: true,
            saveCallback: Save);
        
        return false;
    }

    private List<ElementField> TabGeneral(Script item)
    {
        List<ElementField> fields = new List<ElementField>();

        fields.Add(new ElementField
        {
            InputType = FormInputType.Label,
            Name = "InternalProcessingNodeDescription"
        });
        
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<FileFlows.Shared.Validators.Validator>
            {
                new FileFlows.Shared.Validators.Required()
            }
        });

        
        return fields;
    }


    private List<ElementField> TabOutputs(Script item)
    {
        List<ElementField> fields = new List<ElementField>();

        fields.Add(new ElementField
        {
            InputType = FormInputType.Label,
            Name = "OutputsDescription"
        });
        
        var efTable = new ElementField
        {
            InputType = FormInputType.Table,
            Name = nameof(item.Outputs),
            Parameters = new ()
            {
                { nameof(InputTable.TableType), typeof(ScriptOutput) },
                {
                    "Columns", new List<InputTableColumn>
                    {
                        new () { Property = nameof(ScriptOutput.Output) },
                        new () { Property = nameof(ScriptOutput.Description) },
                    }
                }
            }
        };
        
        fields.Add(efTable);
        
        return fields;
    }
}