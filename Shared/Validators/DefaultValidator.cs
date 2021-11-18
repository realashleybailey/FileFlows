namespace FileFlows.Shared.Validators
{
    using System.Threading.Tasks;

    /// <summary>
    /// Used instead of null
    /// </summary>
    public class DefaultValidator : Validator
    {
        public async override Task<bool> Validate(object value)
        {
            await Task.CompletedTask;
            return true;
        }
    }

}