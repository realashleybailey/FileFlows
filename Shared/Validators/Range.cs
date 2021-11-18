using System;
using System.Threading.Tasks;

namespace FileFlows.Shared.Validators
{
    public class Range : Validator
    {
        public int Minimum { get; set; }
        public int Maximum { get; set; }

        public async override Task<bool> Validate(object value)
        {
            await Task.CompletedTask;

            if (value is Int64 i64)
                return i64 >= Minimum && i64 <= Maximum;
            if (value is Int32 i32)
                return i32 >= Minimum && i32 <= Maximum;
            return true;
        }
    }

}