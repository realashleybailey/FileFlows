using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.Plugin.Attributes
{
    public class ConditionEqualsAttribute:Attribute
    {
        public string Property { get; set; }
        public object Value { get; set; }
        public bool Inverse{ get; set; }

        public ConditionEqualsAttribute(string property, object value, bool inverse = false)
        {
            this.Property = property;
            this.Value = value;
            this.Inverse = inverse; 
        }
    }
}
