using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileFlows.Shared.Validators
{
    public class Pattern : Validator
    {
        public string Expression { get; set; }

        public async override Task<bool> Validate(object value)
        {
            await Task.CompletedTask;

            if (string.IsNullOrEmpty(Expression))
                return true;

            var regex = new Regex(Expression);
            return regex.IsMatch(value as string ?? "");
        }
    }

}