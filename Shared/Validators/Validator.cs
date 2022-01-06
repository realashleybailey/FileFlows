using System.Threading.Tasks;

namespace FileFlows.Shared.Validators
{
    [System.Text.Json.Serialization.JsonConverter(typeof(Json.ValidatorConverter))]
    public abstract class Validator
    {
        public string Type => this.GetType().Name;

        public virtual async Task<bool> Validate(object value) => await Task.FromResult(true);
    }

}