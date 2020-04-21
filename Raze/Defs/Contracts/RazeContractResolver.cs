using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Raze.Defs.Contracts
{
    public class RazeContractResolver : DefaultContractResolver
    {
        public static readonly RazeContractResolver Instance = new RazeContractResolver();

        private RazeContractResolver()
        {

        }

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
