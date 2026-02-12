using System.Reflection;
using System.Runtime.Serialization;
using DataServer.Common.Mapping;

namespace DataServer.Common.Extensions;

public static class EnumMemberExtensions
{
    public static bool TryParseEnumMember<TEnum>(this string value, out TEnum result)
        where TEnum : struct, Enum => EnumMemberMapper<TEnum>.TryParse(value, out result);

    public static string ToEnumMemberValue(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null)
            return value.ToString();

        var field = type.GetField(name);
        if (field == null)
            return value.ToString();

        var attribute = field.GetCustomAttribute<EnumMemberAttribute>();

        return attribute?.Value ?? value.ToString();
    }
}
