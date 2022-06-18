using BlazorDateRangePicker;

namespace FileFlows.Client.Helpers;

/// <summary>
/// Helper for DateRange used in search forms
/// </summary>
public class DateRangeHelper
{
    /// <summary>
    /// The date used for the Live Start point
    /// </summary>
    public static readonly DateTime LiveStart = DateTime.Today.AddDays(-7);
    /// <summary>
    /// The data used for the Live End point
    /// </summary>
    public static readonly DateTime LiveEnd = DateTime.Today.AddDays(7);
    
    private static Dictionary<string, DateRange> _LiveDateRanges;

    /// <summary>
    /// Gets the data ranges used in search forms including the Live option
    /// </summary>
    public static Dictionary<string, DateRange> LiveDateRanges
    {
        get
        {
            if (_LiveDateRanges == null)
            {
                _LiveDateRanges = new Dictionary<string, DateRange>
                {
                    {
                        Translater.Instant("Labels.DateRanges.Live"), new DateRange
                        {
                            Start = LiveStart,
                            End = LiveEnd
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Today"), new DateRange
                        {
                            Start = DateTime.Today,
                            End = DateTime.Today.AddDays(1).AddTicks(-1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Yesterday"), new DateRange
                        {
                            Start = DateTime.Today.AddDays(-1),
                            End = DateTime.Today.AddTicks(-1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last24Hours"), new DateRange
                        {
                            Start = DateTime.Now.AddDays(-1),
                            End = DateTime.Now.AddHours(1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last3Days"), new DateRange
                        {
                            Start = DateTime.Now.AddDays(-3),
                            End = DateTime.Now.AddHours(1)
                        }
                    },
                };


            }

            return _LiveDateRanges;
        }
    }

    private static Dictionary<string, DateRange> _DateRanges;
    
    /// <summary>
    /// Gets the data ranges used in search forms
    /// </summary>
    public static Dictionary<string, DateRange> DateRanges
    {
        get
        {
            if (_DateRanges == null)
            {
                _DateRanges = new Dictionary<string, DateRange>
                    {
                        {
                            Translater.Instant("Labels.DateRanges.AnyTime"), new DateRange
                            {
                                Start = DateTimeOffset.MinValue,
                                End = DateTimeOffset.MaxValue
                            }
                        },
                        {
                            Translater.Instant("Labels.DateRanges.Today"), new DateRange
                            {
                                Start = DateTime.Today,
                                End = DateTime.Today.AddDays(1).AddTicks(-1)
                            }
                        },
                        {
                            Translater.Instant("Labels.DateRanges.Yesterday"), new DateRange
                            {
                                Start = DateTime.Today.AddDays(-1),
                                End = DateTime.Today.AddTicks(-1)
                            }
                        },
                        {
                            Translater.Instant("Labels.DateRanges.Last24Hours"), new DateRange
                            {
                                Start = DateTime.Now.AddDays(-1),
                                End = DateTime.Now.AddHours(1)
                            }
                        },
                        {
                            Translater.Instant("Labels.DateRanges.Last3Days"), new DateRange
                            {
                                Start = DateTime.Now.AddDays(-3),
                                End = DateTime.Now.AddHours(1)
                            }
                        },
                    };


            }

            return _DateRanges;
        }
    }   
}