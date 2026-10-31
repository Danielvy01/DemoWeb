using Microsoft.Data.SqlClient;

namespace Siserp.Api.Infrastructure.Persistence;

/// <summary>Helpers para leer valores desde un <see cref="SqlDataReader"/> tolerando DBNull.</summary>
internal static class SqlDataReaderExtensions
{
    public static string? GetStringOrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static Guid? GetGuidOrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    public static DateTime? GetDateTimeOrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    public static decimal? GetDecimalOrNull(this SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    public static object AsDbValue(this object? value) => value ?? DBNull.Value;
}
