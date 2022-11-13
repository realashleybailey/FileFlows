using FileFlows.Plugin;
using FileFlows.Client.Components.Inputs;

namespace FileFlows.Client.Pages;

public partial class Libraries : ListPage<Guid, Library>
{
    ElementField efTemplate;

    private async Task<bool> OpenEditor(Library library)
    {
        Blocker.Show();
        var flowResult = await GetFlows();
        Blocker.Hide();
        if (flowResult.Success == false || flowResult.Data?.Any() != true)
        {
            ShowEditHttpError(flowResult, "Pages.Libraries.ErrorMessages.NoFlows");
            return false;
        }
        var flowOptions = flowResult.Data.Where(x => x.Type != FlowType.Failure).Select(x => new ListOption { Value = new ObjectReference { Name = x.Name, Uid = x.Uid, Type = x.GetType().FullName }, Label = x.Name });
        efTemplate = null;

        var tabs = new Dictionary<string, List<ElementField>>();
        var tabGeneral = await TabGeneral(library, flowOptions);
        tabs.Add("General", tabGeneral);
        tabs.Add("Schedule", TabSchedule(library));
        tabs.Add("Detection", TabDetection(library));
        tabs.Add("Advanced", TabAdvanced(library));
        var result = await Editor.Open(new()
        {
            TypeName = "Pages.Library", Title = "Pages.Library.Title", Model = library, SaveCallback = Save, Tabs = tabs
        });
        if (efTemplate != null)
        {
            efTemplate.ValueChanged -= TemplateValueChanged;
            efTemplate = null;
        }
        return true;
    }


    private async Task<List<ElementField>> TabGeneral(Library library, IEnumerable<ListOption> flowOptions)
    {
        List<ElementField> fields = new List<ElementField>();
#if (!DEMO)
        if (library == null || library.Uid == Guid.Empty)
        {
            // adding
            Blocker.Show();
            try
            {
                var templateResult = await HttpHelper.Get<Dictionary<string, List<Library>>>("/api/library/templates");
                if (templateResult.Success == true || templateResult.Data?.Any() == true)
                {
                    List<ListOption> templates = new();
                    foreach (var group in templateResult.Data)
                    {
                        if (string.IsNullOrEmpty(group.Key) == false)
                        {
                            templates.Add(new ListOption
                            {
                                Value = Globals.LIST_OPTION_GROUP,
                                Label = group.Key
                            });
                        }
                        templates.AddRange(group.Value.Select(x => new ListOption
                        {
                            Label = x.Name,
                            Value = x
                        }));
                    }
                    var loCustom = new ListOption
                    {
                        Label = "Custom",
                        Value = null
                    };
                    
                    if(templates.Any() && templates[0].Value == Globals.LIST_OPTION_GROUP)
                        templates.Insert(1, loCustom);
                    else
                        templates.Insert(0, loCustom);
                    
                    efTemplate = new ElementField
                    {
                        Name = "Template",
                        InputType = FormInputType.Select,
                        Parameters = new Dictionary<string, object>
                        {
                            { nameof(InputSelect.Options), templates },
                            { nameof(InputSelect.AllowClear), false},
                            { nameof(InputSelect.ShowDescription), true }
                        }
                    };
                    efTemplate.ValueChanged += TemplateValueChanged;
                    fields.Add(efTemplate);
                    fields.Add(new ElementField
                    {
                        InputType = FormInputType.HorizontalRule
                    });
                }
            }
            finally
            {
                Blocker.Hide();
            }
        }
#endif
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(library.Name),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Folder,
            Name = nameof(library.Path),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.Flow),
            Parameters = new Dictionary<string, object>{
                { "Options", flowOptions.ToList() }
            },
            Validators = new List<FileFlows.Shared.Validators.Validator>
            {
                new FileFlows.Shared.Validators.Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.Priority),
            Parameters = new Dictionary<string, object>{
                { "AllowClear", false },
                { "Options", new List<ListOption> {
                    new () { Value = ProcessingPriority.Lowest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Lowest)}" },
                    new () { Value = ProcessingPriority.Low, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Low)}" },
                    new () { Value = ProcessingPriority.Normal, Label =$"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Normal)}" },
                    new () { Value = ProcessingPriority.High, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.High)}" },
                    new () { Value = ProcessingPriority.Highest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Highest)}" }
                } }
            }
        });
        
        if(App.Instance.FileFlowsSystem.Licensed)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Select,
                Name = nameof(library.ProcessingOrder),
                Parameters = new Dictionary<string, object>{
                    { "AllowClear", false },
                    { "Options", new List<ListOption> {
                        new () { Value = ProcessingOrder.AsFound, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.AsFound)}" },
                        new () { Value = ProcessingOrder.LargestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.LargestFirst)}" },
                        new () { Value = ProcessingOrder.NewestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.NewestFirst)}" },
                        new () { Value = ProcessingOrder.Random, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Random)}" },
                        new () { Value = ProcessingOrder.SmallestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.SmallestFirst)}" },
                    } }
                }
            });
        }
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(library.HoldMinutes)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.Enabled)
        });
        
        return fields;
    }

    private List<ElementField> TabSchedule(Library library)
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
            Name = nameof(library.Schedule),
            Parameters = new Dictionary<string, object>
            {
                { "HideLabel", true }
            }
        });
        return fields;
    }

    private List<ElementField> TabAdvanced(Library library)
    {
        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(library.Filter)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(library.ExclusionFilter)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.ExcludeHidden)
        });
        ElementField efFolders = new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.Folders)
        };
        fields.Add(efFolders);
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.SkipFileAccessTests),
            Conditions = new List<Condition>
            {
                new (efFolders, library.Folders, value: false)
            }
            
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.ReprocessRecreatedFiles)
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.UseFingerprinting),
            Conditions = new List<Condition>
            {
                new (efFolders, library.Folders, value: false)
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Name = nameof(library.WaitTimeSeconds),
            Conditions = new List<Condition>
            {
                new Condition(efFolders, library.Folders, value: true)                    
            }
        });
        var fieldScan = new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(library.Scan)
        };
        fields.Add(fieldScan);
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Parameters = new Dictionary<string, object>
            {
                { "Min", 10 },
                { "Max", 24 * 60 * 60 }
            },
            Name = nameof(library.ScanInterval),
            DisabledConditions = new List<Condition>
            {
                new EmptyCondition(fieldScan, library.Scan)
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Int,
            Parameters = new Dictionary<string, object>
            {
                { "Min", 0 },
                { "Max", 300 }
            },
            Name = nameof(library.FileSizeDetectionInterval),
            DisabledConditions = new List<Condition>
            {
                new EmptyCondition(fieldScan, library.Scan)
            }
        });
        return fields;
    }

    private List<ElementField> TabDetection(Library library)
    {
        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ()
        {
            InputType = FormInputType.Label,
            Name = "DetectionDescription"
        });
        var efPeriod = new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.DetectionPeriod),
            Parameters = new Dictionary<string, object>{
                { "AllowClear", false },
                { "Options", new List<ListOption> {
                    new () { Value = DetectionPeriod.None, Label = $"Enums.{nameof(DetectionPeriod)}.{nameof(DetectionPeriod.None)}" },
                    new () { Value = DetectionPeriod.NewerThan, Label = $"Enums.{nameof(DetectionPeriod)}.{nameof(DetectionPeriod.NewerThan)}" },
                    new () { Value = DetectionPeriod.OlderThan, Label = $"Enums.{nameof(DetectionPeriod)}.{nameof(DetectionPeriod.OlderThan)}" }
                } }
            }
        };
        fields.Add(efPeriod);
        fields.Add(new ElementField
        {
            InputType = FormInputType.Period,
            Name = nameof(library.DetectionMinutes),
            Conditions = new List<Condition>
            {
                new Condition(efPeriod, library.DetectionPeriod, value: DetectionPeriod.None, isNot: true)                    
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.DetectionLastWriteTime),
            Parameters = new Dictionary<string, object>{
                { "AllowClear", false },
                { "Options", new List<ListOption> {
                    new () { Value = false, Label = $"Pages.{nameof(Library)}.Labels.CreationTime" },
                    new () { Value = true, Label = $"Pages.{nameof(Library)}.Labels.LastWriteTime" },
                } }
            },
            Conditions = new List<Condition>
            {
                new Condition(efPeriod, library.DetectionPeriod, value: DetectionPeriod.None, isNot: true)                    
            }
        });
        
        return fields;
    }
}
