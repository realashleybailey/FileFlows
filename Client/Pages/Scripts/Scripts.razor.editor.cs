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

        List<ElementField> fields = new List<ElementField>();

        if (string.IsNullOrEmpty(item.Code))
        {
            item.Code = @"
/**
 * Description of this script
 * @param {int} NumberParameter Description of this input
 * @output Description of output 1
 * @output Description of output 2
 */
function Script(NumberParameter)
{
    return 1;
}
".Trim();
        }

        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<FileFlows.Shared.Validators.Validator>
            {
                new FileFlows.Shared.Validators.Required()
            }
        });

        
        fields.Add(new ElementField
        {
            InputType = FormInputType.Code,
            Name = "Code",
            Validators = new List<FileFlows.Shared.Validators.Validator>
            {
                new FileFlows.Shared.Validators.ScriptValidator()
            }
        });

        var result = await Editor.Open("Pages.Script", "Pages.Script.Title", fields, item, large: true,
            saveCallback: Save, helpUrl: "https://github.com/revenz/FileFlows/wiki/Scripts");
        
        return false;
    }
}