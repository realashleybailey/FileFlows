using System.Text.Json;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Widgets;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dashboard;

public partial class CustomDashboard 
{
    private bool DoesntHaveWidget(Guid uid)
    {
        return Widgets?.Any(x => x.Uid == uid) != true;
    }
    
    private async Task AddWidgetDialog()
    {
        List<ElementField> fields = new List<ElementField>();

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.CpuUsage.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.CpuUsage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TimeSeries }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.MemoryUsage.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.MemoryUsage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TimeSeries }
                }
            });
        }


        if (DoesntHaveWidget(FileFlows.Shared.Widgets.LogStorage.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.LogStorage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TimeSeries }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.TempStorage.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.TempStorage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TimeSeries }
                }
            });
        }

        if (App.Instance.FileFlowsSystem.ExternalDatabase && DoesntHaveWidget(FileFlows.Shared.Widgets.OpenDatabaseConnections.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.OpenDatabaseConnections),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TimeSeries }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.Processing.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.Processing),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.Processing }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.FilesUpcoming.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.FilesUpcoming),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.LibraryFileTable }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.FilesRecentlyFinished.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.FilesRecentlyFinished),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.LibraryFileTable }
                }
            });
        }
        
        if (DoesntHaveWidget(FileFlows.Shared.Widgets.StorageSaved.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.StorageSaved),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.Bar }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.Codecs.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.Codecs),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TreeMap }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.AudioCodecs.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.AudioCodecs),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TreeMap }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.VideoCodecs.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.VideoCodecs),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.TreeMap }
                }
            });
        }

        if (App.Instance.FileFlowsSystem.ExternalDatabase && DoesntHaveWidget(FileFlows.Shared.Widgets.ProcessingTimes.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.ProcessingTimes),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.HeatMap }
                }
            });
        }

        if (App.Instance.FileFlowsSystem.ExternalDatabase && DoesntHaveWidget(FileFlows.Shared.Widgets.LibraryProcessingTimes.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.LibraryProcessingTimes),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.BoxPlot }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.VideoContainers.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.VideoContainers),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.PieChart }
                }
            });
        }

        if (DoesntHaveWidget(FileFlows.Shared.Widgets.VideoResolution.WD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Widget,
                Name = nameof(FileFlows.Shared.Widgets.VideoResolution),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputWidget.Type), WidgetType.PieChart }
                }
            });
        }
        var origModel = new AddWidgetModel();
        var result = await Editor.Open("Pages.Widget", "Pages.Widget.Title", fields, null, lblSave: "Labels.Add", saveCallback:
            async (model) =>
            {
                var dict = model as IDictionary<string, object>;
                var dotNetObjRef = DotNetObjectReference.Create(this);
                if (dict == null)
                    return true;
            
                var newWidgets = new List<WidgetUiModel>();
                foreach (var key in dict.Keys)
                {
                    if (dict[key] as bool? != true)
                        continue;
                    
                    switch (key)
                    {
                        case nameof(CpuUsage):
                            newWidgets.Add(CreateNewWidgetModel(CpuUsage.WD_UID, 3, 1));
                            break;
                        case nameof(MemoryUsage):
                            newWidgets.Add(CreateNewWidgetModel(MemoryUsage.WD_UID, 3, 1));
                            break;
                        case nameof(LogStorage):
                            newWidgets.Add(CreateNewWidgetModel(LogStorage.WD_UID, 3, 1));
                            break;
                        case nameof(TempStorage):
                            newWidgets.Add(CreateNewWidgetModel(TempStorage.WD_UID, 3, 1));
                            break;
                        case nameof(OpenDatabaseConnections):
                            newWidgets.Add(CreateNewWidgetModel(OpenDatabaseConnections.WD_UID, 3, 1));
                            break;
                        case nameof(Processing):
                            newWidgets.Add(CreateNewWidgetModel(Processing.WD_UID, 12, 1));
                            break;
                        case nameof(FilesUpcoming):
                            newWidgets.Add(CreateNewWidgetModel(FilesUpcoming.WD_UID, 6, 2));
                            break;
                        case nameof(FilesRecentlyFinished):
                            newWidgets.Add(CreateNewWidgetModel(FilesRecentlyFinished.WD_UID, 6, 2));
                            break;
                        case nameof(StorageSaved):
                            newWidgets.Add(CreateNewWidgetModel(StorageSaved.WD_UID, 6, 2));
                            break;
                        case nameof(Codecs):
                            newWidgets.Add(CreateNewWidgetModel(Codecs.WD_UID, 6, 2));
                            break;
                        case nameof(VideoCodecs):
                            newWidgets.Add(CreateNewWidgetModel(VideoCodecs.WD_UID, 6, 2));
                            break;
                        case nameof(AudioCodecs):
                            newWidgets.Add(CreateNewWidgetModel(AudioCodecs.WD_UID, 6, 2));
                            break;
                        case nameof(ProcessingTimes):
                            newWidgets.Add(CreateNewWidgetModel(ProcessingTimes.WD_UID, 6, 2));
                            break;
                        case nameof(VideoContainers):
                            newWidgets.Add(CreateNewWidgetModel(VideoContainers.WD_UID, 4, 2));
                            break;
                        case nameof(VideoResolution):
                            newWidgets.Add(CreateNewWidgetModel(VideoResolution.WD_UID, 4, 2));
                            break;
                        case nameof(LibraryProcessingTimes):
                            newWidgets.Add(CreateNewWidgetModel(LibraryProcessingTimes.WD_UID, 4, 2));
                            break;
                    }
                }
                if (newWidgets.Any())
                {
                    this.Widgets.AddRange(newWidgets);
                    await jsCharts.InvokeVoidAsync($"addWidgets", ActiveDashboardUid, newWidgets, dotNetObjRef); 
                    var gridWidgets = await jsCharts.InvokeAsync<WidgetUiModel[]>($"getGridData", ActiveDashboardUid, dotNetObjRef);
                    await SaveDashboard(ActiveDashboardUid, gridWidgets);
                }
                return true;
            });
    }

    private WidgetUiModel CreateNewWidgetModel(Guid widgetDefinitionUid, int width, int height)
    {
        var wd = WidgetDefinition.GetDefinition(widgetDefinitionUid);
        var wui = new WidgetUiModel()
        {
            Height = height,
            Width = width,
            Uid = widgetDefinitionUid,

            Flags = wd.Flags,
            Name = wd.Name,
            Type = wd.Type,
            Url = wd.Url,
            Icon = wd.Icon
        };
#if(DEBUG)
        wui.Url = "http://localhost:6868" + wui.Url;
#endif
        return wui;
    }
}

public class AddWidgetModel
{
    public bool CpuUsage { get; set; }
    public bool MemoryUsage { get; set; }
    public bool LogStorage { get; set; }
    public bool TempStorage { get; set; }
    public bool Processing { get; set; }
    public bool FilesUpcoming { get; set; }
    public bool FilesRecentlyFinished { get; set; }
    public bool Codecs { get; set; }
    public bool AudioCodecs { get; set; }
    public bool VideoCodecs { get; set; }
    public bool ProcessingTimes { get; set; }
    public bool LibraryProcessingTimes { get; set; }
    public bool VideoContainers { get; set; }
    public bool VideoResolution { get; set; }
}