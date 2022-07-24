using FileFlows.Client.Components;

namespace FileFlows.Client.Pages;

public partial class Tasks: ListPage<Guid, ScheduledTask>
{
    public override string ApiUrl => "/api/task";
    
    private async Task Add()
    {
        await Edit(new ScheduledTask());
    }
    
    public override async Task<bool> Edit(ScheduledTask item)
    {
        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Text,
            Name = nameof(item.Script),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });
        var result = await Editor.Open("Pages.Task", "Pages.Task.Title", fields, item,
            saveCallback: Save);
        
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
            var saveResult = await HttpHelper.Post<ScheduledTask>($"{ApiUrl}", model);
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