using System.Dynamic;

namespace RailworksForge.Core.Extensions;

public static class ExpandoObjectExtensions
{
    public static KeyValuePair<string, object?> Get(this ExpandoObject expandoObject, string key)
    {
        if (expandoObject.FirstOrDefault(prop => prop.Key == key) is var element)
        {
            return element;
        }

        throw new Exception($"Could not find value for key {key} in expando object");
    }
}
