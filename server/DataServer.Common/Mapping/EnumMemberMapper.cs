using System.Reflection;
using System.Runtime.Serialization;

namespace DataServer.Common.Mapping;

public static class EnumMemberMapper<TEnum>
    where TEnum : struct, Enum
{
    public static readonly Dictionary<string, TEnum> FromString = typeof(TEnum)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f =>
        {
            var attr = f.GetCustomAttribute<EnumMemberAttribute>();
            return new { Key = attr?.Value?.ToLowerInvariant(), Value = (TEnum)f.GetValue(null)! };
        })
        .Where(x => x.Key != null)
        .ToDictionary(x => x.Key!, x => x.Value);

    public static bool TryParse(string value, out TEnum result) =>
        FromString.TryGetValue(value.ToLowerInvariant(), out result);
}
