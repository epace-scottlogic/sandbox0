using System.Reflection;
using System.Runtime.Serialization;
using DataServer.Domain.Blockchain;

namespace DataServer.Api.Utilities;

public static class SymbolMapper
{
    private static readonly Dictionary<string, Symbol> FromString =
        typeof(Symbol).GetFields()
            .Where(f => f.IsLiteral)
            .Select(f =>
            {
                var attr = f.GetCustomAttribute<EnumMemberAttribute>();
                return new
                {
                    Key = attr?.Value?.ToLowerInvariant(),
                    Value = (Symbol)f.GetValue(null)!
                };
            })
            .Where(x => x.Key != null)
            .ToDictionary(x => x.Key!, x => x.Value);

    public static bool TryParse(string value, out Symbol symbol)
        => FromString.TryGetValue(value.ToLowerInvariant(), out symbol);
}