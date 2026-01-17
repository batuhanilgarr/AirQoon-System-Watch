using AirQoon.Web.Models.Dtos;
using Npgsql;
using NpgsqlTypes;

namespace AirQoon.Web.Services;

public class PostgresAirQualityService : IPostgresAirQualityService
{
    private readonly string _connectionString;

    public PostgresAirQualityService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AirQualityConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:AirQualityConnection is missing.");
    }

    public IReadOnlyList<string> NormalizePollutants(IEnumerable<string> pollutants)
    {
        var list = new List<string>();

        foreach (var p in pollutants ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }

            var trimmed = p.Trim();
            var upper = trimmed.ToUpperInvariant();

            if (upper == "PM10")
            {
                list.Add("PM10-24h");
            }
            else if (upper == "PM2.5" || upper == "PM25")
            {
                list.Add("PM2.5-24h");
            }
            else if (upper == "NO2")
            {
                list.Add("NO2-1h");
            }
            else if (upper == "O3")
            {
                list.Add("O3-1h");
            }
            else if (upper == "SO2")
            {
                list.Add("SO2-1h");
            }
            else if (upper == "CO")
            {
                list.Add("CO-8h");
            }
            else
            {
                list.Add(trimmed);
            }
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<AirQualityAggregate>> GetAggregatesAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        IReadOnlyList<string> pollutants,
        CancellationToken cancellationToken = default)
    {
        if (deviceIds == null || deviceIds.Count == 0)
        {
            return Array.Empty<AirQualityAggregate>();
        }

        var normalized = NormalizePollutants(pollutants);
        if (normalized.Count == 0)
        {
            return Array.Empty<AirQualityAggregate>();
        }

        startDateUtc = EnsureUtc(startDateUtc);
        endDateUtc = EnsureUtc(endDateUtc);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = @"
SELECT
    parameter,
    AVG(concentration) AS avg_concentration,
    MIN(concentration) AS min_concentration,
    MAX(concentration) AS max_concentration,
    COUNT(*) AS measurement_count,
    MAX(concentration_unit) AS concentration_unit
FROM air_quality_index
WHERE device_id = ANY(@device_ids)
  AND calculated_datetime >= @start_ts
  AND calculated_datetime < @end_ts
  AND parameter = ANY(@parameters)
GROUP BY parameter
ORDER BY parameter;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("device_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = deviceIds.ToArray()
        });
        cmd.Parameters.Add(new NpgsqlParameter("start_ts", NpgsqlDbType.TimestampTz) { Value = startDateUtc });
        cmd.Parameters.Add(new NpgsqlParameter("end_ts", NpgsqlDbType.TimestampTz) { Value = endDateUtc });
        cmd.Parameters.Add(new NpgsqlParameter("parameters", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = normalized.ToArray()
        });

        var results = new List<AirQualityAggregate>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            static double? ReadDouble(NpgsqlDataReader r, string name)
            {
                var ordinal = r.GetOrdinal(name);
                if (r.IsDBNull(ordinal))
                {
                    return null;
                }

                var val = r.GetValue(ordinal);
                return val switch
                {
                    double d => d,
                    float f => f,
                    decimal m => (double)m,
                    int i => i,
                    long l => l,
                    _ => Convert.ToDouble(val)
                };
            }

            results.Add(new AirQualityAggregate
            {
                Parameter = reader.GetString(reader.GetOrdinal("parameter")),
                Average = ReadDouble(reader, "avg_concentration"),
                Minimum = ReadDouble(reader, "min_concentration"),
                Maximum = ReadDouble(reader, "max_concentration"),
                MeasurementCount = reader.GetInt64(reader.GetOrdinal("measurement_count")),
                Unit = reader.IsDBNull(reader.GetOrdinal("concentration_unit")) ? null : reader.GetString(reader.GetOrdinal("concentration_unit"))
            });
        }

        return results;
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => dt
        };
    }
}
