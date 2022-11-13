namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A Input Period GUI element that will allow for entering a period in minutes
/// </summary>
public class PeriodAttribute : FormInputAttribute
{
    public PeriodAttribute(int order) : base(FormInputType.Period, order) { }
}
