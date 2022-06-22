using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace FileFlows.Client.Components.Dashboard;

/// <summary>
/// Portlet that shows a line data stream
/// </summary>
public partial class SystemValueLineChart<TItem>:ComponentBase
{
    private string _Title = string.Empty;
    
    // green:
    private string colorGreen = "#00ff00";
    // blue: 
    private string colorBlue = "#00BAEC";
    
    /// <summary>
    /// Gets or sets the title 
    /// </summary>
    [Parameter] public string Title
    {
        get => _Title;
        set
        {
            _Title = Translater.TranslateIfNeeded(value);
        }
    }
    
    /// <summary>
    /// Gets or sets the icon to show
    /// </summary>
    [Parameter] public string Icon { get; set; }

    [Parameter] public List<SystemValue<TItem>> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets if this data is size data, eg KB, MB, GB etc
    /// </summary>
    [Parameter] public bool SizeData { get; set; }

    private ApexChart<SystemValue<TItem>> chartTop;
    private ApexChart<SystemValue<TItem>> chartBottom;
    
    ApexChartOptions<SystemValue<TItem>> chartTopOptions;
    ApexChartOptions<SystemValue<TItem>> chartBottomOptions;
    
    protected override async Task OnInitializedAsync()
    {
        string mainChartId = Guid.NewGuid().ToString();
        chartTopOptions = new ()
        {
            Title = new()
            {
                Text = null
            },
            Chart = new()
            {
                Background = "transparent"
            },
            Xaxis = new()
            {
                
                AxisTicks = new ()
                {
                    Show = false
                },
              Labels  = new ()
              {
                  Show = false,
                  DatetimeUTC = false
              }
            },
            Yaxis = new()
            {
                new ()
                {
                    Show =false
                } 
            },
            Grid = new()
            {
                Padding = new Padding()
                {
                    Bottom = 0,
                    Left = 0,
                    Right = 0,
                    Top = 0
                },
                Show = false
            },
            Stroke = new ()
            {
              Curve = Curve.Smooth
            },
            Fill = new ()
            {
              Gradient  = new()
              {
                  OpacityFrom = 0.55f,
                  OpacityTo = 0f
              }
            },
            Markers = new ()
            {
                //Size = 5,
                Colors = new List<string> {"#00BAEC"},
                StrokeColors =  new ("#00BAEC"),
                StrokeWidth = new (3)
            },
            Theme = new()
            {
                Mode = Mode.Dark,
                Palette = PaletteType.Palette3
            },
            Tooltip = new Tooltip
            {
                X = new ()
                {
                  Format =  "h:mm:ss tt, d MMM yyyy",
                  Show = false
                },
                Y = new()
                {
                    Formatter = SizeData ?
                        @"function(value, opts) {
                    if (value === undefined) {return '';}
                    let sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
                    let order = 0;
                    while (value >= 1000 && order < sizes.length - 1) {
                        order++;
                        value = value / 1000;
                    }
                    return value.toFixed(2) + ' ' + sizes[order];}"
                        :
                        @"function(value, opts) {
                    if (value === undefined) {return '';}
                    return value.toFixed(1) + ' %';}"
                },
                
            }
        };
        chartTopOptions.Chart.Id = mainChartId;
        chartTopOptions.Chart.Toolbar = new Toolbar { AutoSelected = AutoSelected.Pan, Show = false };

        chartBottomOptions = new()
        {
            Title = new()
            {
                Text = string.Empty
            },
            Chart = new ()
            {
                OffsetX = 0,
                OffsetY = 0,
                Background = "transparent",
                Events = new Dictionary<string, object>()
                {
                    {
                        "selection", "console.log('event')"
                    }
                },
                Toolbar = new()
                {
                    
                },
                Selection = new ()
                {
                    Enabled = true,
                    Fill = new ()
                    {
                        Color = "#fff",
                        Opacity = 0.3
                    }
                },
              Animations  = new ()
              {
                  Enabled = false
              }
            },
            Grid = new()
            {
                Padding = new Padding()
                {
                    Bottom = 0,
                    Left = 0,
                    Right = 0,
                    Top = 0
                },
                Show = false
            },
            Xaxis = new()
            {
                AxisTicks = new ()
                {
                  Show = false
                },
                Labels  = new ()
                {
                    Show = false
                }
            },
            Yaxis = new()
            {
                new ()
                {
                    Show = false
                } 
            },
            
            Theme = new()
            {
                Mode = Mode.Dark,
                Palette = PaletteType.Palette3
            },
        };
        //var selectionStart = data.Min(e => e.Date).AddDays(30);
        
        chartBottomOptions.Chart.Toolbar = new Toolbar { Show = false };
        chartBottomOptions.Chart.Brush = new ApexCharts.Brush { Enabled = true, Target = mainChartId };
    }

    public async Task AppendData(IEnumerable<SystemValue<TItem>> data)
    {
        
        bool initial = this.Data?.Any() == false;
        if (initial)
        {
            this.Data = data.ToList();
            this.StateHasChanged();
        }
        else
        {
            this.Data.AddRange(data);
            Console.WriteLine("Data length: " + Data.Count);
            if(chartTop != null)
                await chartTop.AppendDataAsync(data);
            if (chartBottom != null)
                await chartBottom.AppendDataAsync(data);
        }

        // if (this.Data != null)
        // {
        //     var min = this.Data.CpuUsage.Min(e => e.Time);
        //     var max = this.Data.CpuUsage.Max(e => e.Time);
        //     if (cpuChartOptionsBottom != null)
        //     {
        //         cpuChartOptionsBottom.Chart.Selection = new Selection
        //         {
        //             Enabled = true,
        //             Xaxis = new SelectionXaxis
        //             {
        //                 Min = min.ToUnixTimeMilliseconds(),
        //                 Max = max.ToUnixTimeMilliseconds()
        //             }
        //         };
        //     }
        // }
    }
    private void DataPointsSelected(SelectedData<SystemValue<TItem>> selectedData)
    {
        if (!selectedData.IsSelected)
        {
            Console.WriteLine("Nothing seelcted");
            //this.selectedData = null;
            return;
        }

        //this.selectedData = selectedData;
        //detailsChart?.RenderAsync();
        Console.WriteLine("selected data: " + System.Text.Json.JsonSerializer.Serialize(selectedData));
    }
    
    
}