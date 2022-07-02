using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Portlets;

namespace FileFlows.Client.Pages;


public partial class Dashboard 
{
    private async Task AddPortletDialog()
    {
        List<ElementField> fields = new List<ElementField>();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.CpuUsage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.MemoryUsage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.LogStorage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.TempStorage),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TimeSeries } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.Processing),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.Processing } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.FilesUpcoming),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.LibraryFileTable } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.FilesRecentlyFinished),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.LibraryFileTable } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.Codecs),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TreeMap } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.AudioCodecs),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TreeMap } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.VideoCodecs),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.TreeMap } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.ProcessingTimes),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.HeatMap } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.LibraryProcessingTimes),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.BoxPlot } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.VideoContainers),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.PieChart } 
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Portlet,
            Name = nameof(FileFlows.Shared.Portlets.VideoResolution),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputPortlet.Type), PortletType.PieChart } 
            }
        });
        
       
        var result = await Editor.Open("Pages.Portlet", "Pages.Portlet.Title", fields, null, lblSave: "Labels.Add");
        
        
    }
}