using Newtonsoft.Json;
using System;

namespace Raze.Defs
{
    public class DefStub
    {
        public string Name;
        public string Parent;
        public Type Class;

        [JsonProperty("Abstract")]
        internal bool _IsAbstract;
        [JsonProperty("AllowAdditionalData")]
        internal bool AllowAdditionalData;

        [NonSerialized]
        internal DefStub _InternalParent;
        [NonSerialized]
        internal string _Json;

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
