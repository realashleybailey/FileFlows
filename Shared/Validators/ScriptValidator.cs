using System.Threading.Tasks;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Shared.Validators;

/// <summary>
/// Validator that checks a Script object is valid
/// </summary>
public class ScriptValidator:Validator
{
    /// <summary>
    /// Validates a Script
    /// </summary>
    /// <param name="value">the Script code to validate</param>
    /// <returns>true if valid, otherwise false</returns>
    public override async Task<(bool Valid, string Error)> Validate(object value)
    {
        await Task.CompletedTask;
        try
        {
            new ScriptParser().Parse(new Script
            {
                Name = "Validating",
                Code = value as string
            });
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}