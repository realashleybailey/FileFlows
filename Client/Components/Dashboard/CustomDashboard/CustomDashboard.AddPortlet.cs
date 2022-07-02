using System.Text.Json;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Portlets;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dashboard;

public partial class CustomDashboard 
{
    private bool DoesntHavePortlet(Guid uid)
    {
        return Portlets?.Any(x => x.Uid == uid) != true;
    }
    
    private async Task AddPortletDialog()
    {
        List<ElementField> fields = new List<ElementField>();

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.CpuUsage.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.CpuUsage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TimeSeries }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.MemoryUsage.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.MemoryUsage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TimeSeries }
                }
            });
        }


        if (DoesntHavePortlet(FileFlows.Shared.Portlets.LogStorage.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.LogStorage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TimeSeries }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.TempStorage.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.TempStorage),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TimeSeries }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.Processing.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.Processing),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.Processing }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.FilesUpcoming.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.FilesUpcoming),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.LibraryFileTable }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.FilesRecentlyFinished.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.FilesRecentlyFinished),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.LibraryFileTable }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.Codecs.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.Codecs),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TreeMap }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.AudioCodecs.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.AudioCodecs),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TreeMap }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.VideoCodecs.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.VideoCodecs),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.TreeMap }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.ProcessingTimes.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.ProcessingTimes),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.HeatMap }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.LibraryProcessingTimes.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.LibraryProcessingTimes),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.BoxPlot }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.VideoContainers.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.VideoContainers),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.PieChart }
                }
            });
        }

        if (DoesntHavePortlet(FileFlows.Shared.Portlets.VideoResolution.PD_UID))
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Portlet,
                Name = nameof(FileFlows.Shared.Portlets.VideoResolution),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputPortlet.Type), PortletType.PieChart }
                }
            });
        }
        var origModel = new AddPortletModel();
        var result = await Editor.Open("Pages.Portlet", "Pages.Portlet.Title", fields, null, lblSave: "Labels.Add", saveCallback:
            (model) =>
            {
                Console.WriteLine("Save callback 1: " + JsonSerializer.Serialize(model));
                var dict = model as IDictionary<string, object>;
                if (dict != null)
                {
                    Console.WriteLine("Save callback 2: " + dict.Count);
                    var newPortlets = new List<PortletUiModel>();
                    foreach (var key in dict.Keys)
                    {
                        Console.WriteLine("Save callback 2a");
                        if (dict[key] as bool? != true)
                            continue;
                        Console.WriteLine("Save callback 2b");
                        Console.WriteLine("Save callback 2c: " + key);
                        
                        switch (key)
                        {
                            case nameof(CpuUsage):
                                newPortlets.Add(CreateNetPortletModel(CpuUsage.PD_UID, 3, 1));
                                break;
                            case nameof(MemoryUsage):
                                newPortlets.Add(CreateNetPortletModel(MemoryUsage.PD_UID, 3, 1));
                                break;
                            case nameof(LogStorage):
                                newPortlets.Add(CreateNetPortletModel(LogStorage.PD_UID, 3, 1));
                                break;
                            case nameof(TempStorage):
                                newPortlets.Add(CreateNetPortletModel(TempStorage.PD_UID, 3, 1));
                                break;
                            case nameof(Processing):
                                newPortlets.Add(CreateNetPortletModel(Processing.PD_UID, 12, 1));
                                break;
                            case nameof(FilesUpcoming):
                                newPortlets.Add(CreateNetPortletModel(FilesUpcoming.PD_UID, 6, 2));
                                break;
                            case nameof(FilesRecentlyFinished):
                                newPortlets.Add(CreateNetPortletModel(FilesRecentlyFinished.PD_UID, 6, 2));
                                break;
                            case nameof(Codecs):
                                newPortlets.Add(CreateNetPortletModel(Codecs.PD_UID, 6, 2));
                                break;
                            case nameof(VideoCodecs):
                                newPortlets.Add(CreateNetPortletModel(VideoCodecs.PD_UID, 6, 2));
                                break;
                            case nameof(AudioCodecs):
                                newPortlets.Add(CreateNetPortletModel(AudioCodecs.PD_UID, 6, 2));
                                break;
                            case nameof(ProcessingTimes):
                                newPortlets.Add(CreateNetPortletModel(ProcessingTimes.PD_UID, 6, 2));
                                break;
                            case nameof(VideoContainers):
                                newPortlets.Add(CreateNetPortletModel(VideoContainers.PD_UID, 4, 2));
                                break;
                            case nameof(VideoResolution):
                                newPortlets.Add(CreateNetPortletModel(VideoResolution.PD_UID, 4, 2));
                                break;
                            case nameof(LibraryProcessingTimes):
                                newPortlets.Add(CreateNetPortletModel(LibraryProcessingTimes.PD_UID, 4, 2));
                                break;
                        }
                    }
                    
                    Console.WriteLine("Save callback 3");
                    Logger.Instance.ILog("ADding portlets: ", newPortlets );

                    if (newPortlets.Any())
                    {
                        Console.WriteLine("Save callback 4");
                        this.Portlets.AddRange(newPortlets);
                        var dotNetObjRef = DotNetObjectReference.Create(this);
                        jsCharts.InvokeVoidAsync($"addPortlets",  newPortlets, dotNetObjRef); 
                    }
                    Console.WriteLine("Save callback 5");
                }
                Console.WriteLine("Save callback 6");
                return Task.FromResult(true);
            });
    }

    private PortletUiModel CreateNetPortletModel(Guid portlDefinitionUid, int width, int height)
    {
        var pd = PortletDefinition.GetDefinition(portlDefinitionUid);
        var pui = new PortletUiModel()
        {
            Height = height,
            Width = width,
            Uid = portlDefinitionUid,

            Flags = pd.Flags,
            Name = pd.Name,
            Type = pd.Type,
            Url = pd.Url,
            Icon = pd.Icon
        };
#if(DEBUG)
        pui.Url = "http://localhost:6868" + pui.Url;
#endif
        return pui;
    }
}

public class AddPortletModel
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