using DataServer.Common.Mapping;

namespace DataServer.Common.Extensions;

public static class EnumMemberExtensions 
{
    public static bool TryParseEnumMember<TEnum>(this string value, out TEnum result)
        where TEnum : struct, Enum
        => EnumMemberMapper<TEnum>.TryParse(value, out result);
}
