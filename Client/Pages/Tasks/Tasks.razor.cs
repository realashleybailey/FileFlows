using System.Data;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Humanizer;

namespace FileFlows.Client.Pages;

public partial class Tasks: ListPage<Guid, FileFlowsTask>
{
    public override string ApiUrl => "/api/task";

    private enum TimeSchedule
    {
        Hourly,
        Every3Hours,
        Every6Hours,
        Every12Hours,
        Daily,
        Custom
    }

    private static readonly string SCHEDULE_HOURLY = string.Concat(Enumerable.Repeat("1000", 24 * 7));
    private static readonly string SCHEDULE_3_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 11), 8 * 7));
    private static readonly string SCHEDULE_6_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 23), 4 * 7));
    private static readonly string SCHEDULE_12_HOURLY = string.Concat(Enumerable.Repeat("1" + new string('0', 47), 2 * 7));
    private static readonly string SCHEDULE_DAILY = string.Concat(Enumerable.Repeat("1" + new string('0', 95), 7));

    private string GetSchedule(FileFlowsTask task)
    {
        if (task.Type != TaskType.Schedule)
            return string.Empty;
        if (task.Schedule == SCHEDULE_HOURLY) return "Hourly";
        if (task.Schedule == SCHEDULE_3_HOURLY) return "Every 3 Hours";
        if (task.Schedule == SCHEDULE_6_HOURLY) return "Every 6 Hours";
        if (task.Schedule == SCHEDULE_12_HOURLY) return "Every 12 Hours";
        if (task.Schedule == SCHEDULE_DAILY) return "Daily";
        return "Custom Schedule";
    }
    
    private async Task Add()
    {
        await Edit(new FileFlowsTask()
        {
            Schedule = SCHEDULE_DAILY
        });
    }
    
    public override async Task<bool> Edit(FileFlowsTask item)
    {
        List<ElementField> fields = new List<ElementField>();

        var scriptResponse = await HttpHelper.Get<List<Script>>("/api/script/list/system");
        if (scriptResponse.Success == false)
        {
            Toast.ShowError(scriptResponse.Body);
            return false;
        }

        if (scriptResponse.Data?.Any() != true)
        {
            Toast.ShowError("Pages.Tasks.Message.NoScripts");
            return false;
        }

        var scriptOptions = scriptResponse.Data.Select(x => new ListOption()
        {
            Label = x.Name,
            Value = x.Uid
        }).ToList();

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
            InputType = FileFlows.Plugin.FormInputType.Select,
            Name = nameof(item.Script),
            Parameters = new ()
            {
                { nameof(InputSelect.Options), scriptOptions }
            }
        });
        var efTaskType = new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Select,
            Name = nameof(item.Type),
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    Enum.GetValues<TaskType>().Select(x => new ListOption()
                        { Label = x.Humanize(LetterCasing.Title).Replace("File Flows", "FileFlows"), Value = x }).ToList()
                }
            }
        };
        fields.Add(efTaskType);

        TimeSchedule timeSchedule = TimeSchedule.Hourly;
        if (item.Type == TaskType.Schedule)
        {
            if (item.Schedule == SCHEDULE_DAILY)
                timeSchedule = TimeSchedule.Daily;
            else if (item.Schedule == SCHEDULE_12_HOURLY)
                timeSchedule = TimeSchedule.Every12Hours;
            else if (item.Schedule == SCHEDULE_6_HOURLY)
                timeSchedule = TimeSchedule.Every6Hours;
            else if (item.Schedule == SCHEDULE_3_HOURLY)
                timeSchedule = TimeSchedule.Every3Hours;
            else if (item.Schedule == SCHEDULE_HOURLY)
                timeSchedule = TimeSchedule.Hourly;
            else if(item.Schedule != new string('0', 672))
                timeSchedule = TimeSchedule.Custom;
        }

        var efSchedule = new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Select,
            Name = "TimeSchedule",
            Parameters = new()
            {
                { nameof(InputSelect.AllowClear), false },
                {
                    nameof(InputSelect.Options),
                    Enum.GetValues<TimeSchedule>().Select(x => new ListOption()
                        { Label = x.Humanize(LetterCasing.Title), Value = x }).ToList()
                }
            },
            Conditions = new List<Condition>
            {
                new(efTaskType, item.Type, value: TaskType.Schedule)
            }
        };
        fields.Add(efSchedule);

        string customSchedule = SCHEDULE_HOURLY;
        if (item.Type == TaskType.Schedule)
        {
            if (item.Schedule == SCHEDULE_DAILY)
                timeSchedule = TimeSchedule.Daily;
            else if (item.Schedule == SCHEDULE_3_HOURLY)
                timeSchedule = TimeSchedule.Every3Hours;
            else if (item.Schedule == SCHEDULE_6_HOURLY)
                timeSchedule = TimeSchedule.Every6Hours;
            else if (item.Schedule == SCHEDULE_12_HOURLY)
                timeSchedule = TimeSchedule.Every12Hours;
            else if (item.Schedule == SCHEDULE_DAILY)
                timeSchedule = TimeSchedule.Daily;
            else if (item.Schedule == SCHEDULE_HOURLY)
                timeSchedule = TimeSchedule.Hourly;
            else
                timeSchedule = TimeSchedule.Custom;
            customSchedule = item.Schedule?.EmptyAsNull() ?? SCHEDULE_HOURLY;
        }
        
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Schedule,
            Name = "CustomSchedule",
            Parameters = new ()
            {
                { nameof(InputSchedule.HideLabel), true }
            },
            Conditions = new List<Condition>
            {
                new (efTaskType, item.Type, value: TaskType.Schedule),
                new (efSchedule, timeSchedule, value: TimeSchedule.Custom)
            }
        });
        var result = await Editor.Open("Pages.Task", "Pages.Task.Title", fields, new
            {
                item.Uid,
                item.Name,
                item.Script,
                item.Type,
                CustomSchedule = customSchedule,
                TimeSchedule = timeSchedule
            },
            saveCallback: Save);
        
        return false;
    }
    
    
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();
        var task = new FileFlowsTask();
        var dict = model as IDictionary<string, object>;
        task.Name = dict["Name"].ToString();
        task.Script = dict["Script"].ToString();
        task.Uid = (Guid)dict["Uid"];
        task.Type = (TaskType)dict["Type"];
        if (task.Type == TaskType.Schedule)
        {
            var timeSchedule = (TimeSchedule)dict["TimeSchedule"];
            switch (timeSchedule)
            {
                case TimeSchedule.Daily: task.Schedule = SCHEDULE_DAILY;
                    break;
                case TimeSchedule.Hourly: task.Schedule = SCHEDULE_HOURLY;
                    break;
                case TimeSchedule.Every3Hours: task.Schedule = SCHEDULE_3_HOURLY;
                    break;
                case TimeSchedule.Every6Hours: task.Schedule = SCHEDULE_6_HOURLY;
                    break;
                case TimeSchedule.Every12Hours: task.Schedule = SCHEDULE_12_HOURLY;
                    break;
                default:
                    task.Schedule = (string)dict["CustomSchedule"];
                    break;
            }
        }

        try
        {
            var saveResult = await HttpHelper.Post<FileFlowsTask>($"{ApiUrl}", task);
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
    }
}