using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Soluvion.API.Data.Configurations
{
    /// <summary>
    /// Ez a segédosztály tartalmazza az újrahasználható JSONB konvertereket és komparátorokat, 
    /// így nem kell minden entitásnál újra megírni a logikát.
    /// </summary>
    public static class JsonConversionHelper
    {
        public static ValueConverter<Dictionary<string, string>, string> DictionaryConverter =
            new ValueConverter<Dictionary<string, string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>()
            );

        public static ValueComparer<Dictionary<string, string>> DictionaryComparer =
            new ValueComparer<Dictionary<string, string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
            );

        public static ValueConverter<List<string>, string> ListStringConverter =
            new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
            );

        public static ValueComparer<List<string>> ListStringComparer =
            new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );
    }
}