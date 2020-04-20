using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Raze.Defs
{
    /// <summary>
    /// A definition is something that describes a runtime object's properties.
    /// The actual definition class is designed to be serialized to file.
    /// The definition 
    /// </summary>
    public abstract class Def : DefStub
    {
        [NonSerialized]
        public string FilePath;

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; }

        public virtual JToken TryGetAdditionalData(string key)
        {
            if (AdditionalData == null)
                return null;

            return AdditionalData.ContainsKey(key) ? AdditionalData[key] : null;
        }

        public override string ToString()
        {
            return $"[{GetType().Name}] {Name}, parent: {Parent ?? "none"}, file: {new FileInfo(FilePath).Name}, {AdditionalData?.Count ?? 0} additional data items";
        }
    }
}
