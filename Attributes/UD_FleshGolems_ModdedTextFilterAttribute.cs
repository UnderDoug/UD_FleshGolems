using System;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class UD_FleshGolems_ModdedTextFilterAttribute : Attribute
    {
        public string Key;

        public bool Override;

        public UD_FleshGolems_ModdedTextFilterAttribute()
        {
            Key = null;
            Override = false;
        }

        public UD_FleshGolems_ModdedTextFilterAttribute(string Key, bool Override = false)
            : this()
        {
            this.Key = Key;
            this.Override = Override;
        }
    }
}
