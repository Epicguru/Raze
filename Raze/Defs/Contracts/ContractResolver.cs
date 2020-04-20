using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Raze.Defs.Contracts
{
    public class ContractResolver : DefaultContractResolver
    {
        public static readonly ContractResolver Instance = new ContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType == typeof(Type))
            {
                //Console.WriteLine($"Using custom Type loader for {property.DeclaringType.Name}.{property.PropertyName}");
                property.Converter = TypeConverter.Instance;
            }

            return property;
        }
    }
}
