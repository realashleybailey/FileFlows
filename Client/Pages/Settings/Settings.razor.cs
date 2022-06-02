namespace FileFlows.Client.Pages
{
    using FileFlows.Shared.Helpers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;
    using FileFlows.Client.Components;
    using System.Collections.Generic;
    using FileFlows.Shared.Validators;
    using Microsoft.JSInterop;
    using FileFlows.Plugin;

    public partial class Settings : ComponentBase
    {
        [CascadingParameter] Blocker Blocker { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IJSRuntime jsRuntime { get; set; }

        private bool ShowInternalProcessingNdoe { get; set; }

        private bool ShowExternalDatabase { get; set; }

        private bool IsSaving { get; set; }

        private string lblSave, lblSaving, lblHelp, lblGeneral, lblAdvanced, lblNode, lblDatabase, lblInternalProcessingNodeDescription, lblDbDescription, lblTest;

        private FileFlows.Shared.Models.Settings Model { get; set; } = new FileFlows.Shared.Models.Settings();

        private ProcessingNode InternalProcessingNode { get; set; } 

        List<Validator> RequiredValidator = new ();

        private List<ListOption> DbTypes = new()
        {
            new() { Label = "SQLite", Value = DatabaseType.Sqlite },
            new() { Label = "SQL Server", Value = DatabaseType.SqlServer },
            new() { Label = "MySQL", Value = DatabaseType.MySql }
        };

        private object DbType
        {
            get => Model.DbType;
            set
            {
                if (value is DatabaseType dbType)
                {
                    Model.DbType = dbType;
                    if (dbType != DatabaseType.Sqlite && string.IsNullOrWhiteSpace(Model.DbName))
                        Model.DbName = "FileFlows";
                }
            }
        }


        protected override async Task OnInitializedAsync()
        {
            lblSave = Translater.Instant("Labels.Save");
            lblSaving = Translater.Instant("Labels.Saving");
            lblHelp = Translater.Instant("Labels.Help");
            lblAdvanced = Translater.Instant("Labels.Advanced");
            lblGeneral = Translater.Instant("Pages.Settings.Labels.General");
            lblNode = Translater.Instant("Pages.Settings.Labels.InternalProcessingNode");
            lblDatabase = Translater.Instant("Pages.Settings.Labels.Database");
            lblInternalProcessingNodeDescription = Translater.Instant("Pages.Settings.Fields.InternalProcessingNode.Description");
            lblDbDescription = Translater.Instant("Pages.Settings.Fields.Database.Description");
            lblTest = Translater.Instant(("Label.Test"));
            Blocker.Show("Loading Settings");

            RequiredValidator.Add(new Required());
            
#if (!DEMO)
            var response = await HttpHelper.Get<FileFlows.Shared.Models.Settings>("/api/settings");
            if (response.Success)
            {
                this.Model = response.Data;
                this.ShowExternalDatabase = this.Model?.DbAllowed == true;
            }

            var nodesResponse = await HttpHelper.Get<ProcessingNode[]>("/api/node");
            if (nodesResponse.Success)
            {
                this.InternalProcessingNode = nodesResponse.Data.Where(x => x.Address == "FileFlowsServer").FirstOrDefault();
                this.ShowInternalProcessingNdoe = this.InternalProcessingNode != null;
            }

#endif
            Blocker.Hide();
        }

        private async Task Save()
        {
#if (DEMO)
            return;
#else
            this.Blocker.Show(lblSaving);
            this.IsSaving = true;
            try
            {
                await HttpHelper.Put<string>("/api/settings", this.Model);

                if (ShowInternalProcessingNdoe && this.InternalProcessingNode != null)
                {
                    await HttpHelper.Post("/api/node", this.InternalProcessingNode);
                }
            }
            finally
            {
                this.IsSaving = false;
                this.Blocker.Hide();
            }
#endif
        }

        private async Task OpenHelp()
        {
            await jsRuntime.InvokeVoidAsync("open", "https://github.com/revenz/FileFlows/wiki/Settings", "_blank");
        }

        private async Task TestDbConnection()
        {
            string server = Model?.DbServer?.Trim();
            string name = Model?.DbName?.Trim();
            string user = Model?.DbUser?.Trim();
            string password = Model?.DbPassword?.Trim();
            if (string.IsNullOrWhiteSpace(server))
            {
                Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoServer"));
                return;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoName"));
                return;
            }
            if (string.IsNullOrWhiteSpace(user))
            {
                Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoUser"));
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoPassword"));
                return;
            }

            Blocker.Show();
            try
            {
                var result = await HttpHelper.Post<string>("/api/settings/test-db-connection", new
                {
                    server, name, user, password, Type = DbType
                });
                if (result.Success == false)
                    throw new Exception(result.Body);
                if(result.Data != "OK")
                    throw new Exception(result.Data);
                Toast.ShowSuccess(Translater.Instant("Pages.Settings.Messages.Database.TestSuccess"));
            }
            catch (Exception ex)
            {
                Toast.ShowError(ex.Message);
            }
            finally
            {
                Blocker.Hide();
            }

        }
    }
}