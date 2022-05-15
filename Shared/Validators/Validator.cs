using System.Threading.Tasks;

namespace FileFlows.Shared.Validators;

/// <summary>
/// A validator used to validate a value
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(Json.ValidatorConverter))]
public abstract class Validator
{
    /// <summary>
    /// Gets the name of the type of validator
    /// </summary>
    public string Type => this.GetType().Name;

    /// <summary>
    /// Validates a value using the validator
    /// </summary>
    /// <param name="value">the value to validate</param>
    /// <returns>true if the value is valid, otherwise false</returns>
    public virtual async Task<bool> Validate(object value) => await Task.FromResult(true);
}