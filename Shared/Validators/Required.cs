using System;
using System.Collections;
using System.Threading.Tasks;

namespace FileFlow.Shared.Validators
{
    public class Required : Validator
    {
        public async override Task<bool> Validate(object value)
        {
            await Task.CompletedTask;
            if (value == null)
                return false;
            if (value is string str)
                return string.IsNullOrWhiteSpace(str) == false;

            if (value is Array array)
                return array.Length > 0;

            if (value is ICollection collection)
                return collection.Count > 0;

            return true;
        }
    }

}