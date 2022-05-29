namespace FileFlows.Client.Pages;

using FileFlows.Client.Components;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Scripts : ListPage<Script>
{
    public override string ApiUrl => "/api/script";

    const string FileFlowsServer = "FileFlowsServer";

    private Script EditingItem = null;


    private async Task Add()
    {
#if (!DEMO)
        await Edit(new Script());
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
            var saveResult = await HttpHelper.Post<Script>($"{ApiUrl}", model);
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