using AirQoon.Web.Models.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using NpgsqlTypes;

namespace AirQoon.Web.Services;

public class PostgresAirQualityService : IPostgresAirQualityService
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;

    public PostgresAirQualityService(IConfiguration configuration, IMemoryCache cache)
    {
        _cache = cache;
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

    public async Task<IReadOnlyList<TelemetryAverage>> GetTelemetryAveragesAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default)
    {
        if (deviceIds == null || deviceIds.Count == 0)
        {
            return Array.Empty<TelemetryAverage>();
        }

        if (string.IsNullOrWhiteSpace(avgType))
        {
            avgType = "24h_rolling";
        }

        startDateUtc = EnsureUtc(startDateUtc);
        endDateUtc = EnsureUtc(endDateUtc);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = @"
SELECT
    device_id,
    calculateddatetime,
    inserteddatetime,
    avgtype,
    pm10calibrated,
    pm25calibrated,
    no2ugm3calibratedfiltered,
    o3ugm3calibratedfiltered,
    so2ugm3calibratedfiltered,
    cougm3calibratedfiltered,
    no2ppbcalibratedfiltered,
    o3ppbcalibratedfiltered,
    so2ppbcalibratedfiltered,
    coppbcalibratedfiltered,
    vocppbrawfiltered,
    humidity,
    temperature,
    pressure,
    windspeed,
    winddirectionfiltered,
    noisecalibrated,
    h2sppbrawfiltered,
    co2ppm
FROM telemetry_averages
WHERE device_id = ANY(@device_ids)
  AND calculateddatetime >= @start_ts
  AND calculateddatetime < @end_ts
  AND avgtype = @avg_type
ORDER BY calculateddatetime DESC;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("device_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = deviceIds.ToArray()
        });
        cmd.Parameters.Add(new NpgsqlParameter("start_ts", NpgsqlDbType.TimestampTz) { Value = startDateUtc });
        cmd.Parameters.Add(new NpgsqlParameter("end_ts", NpgsqlDbType.TimestampTz) { Value = endDateUtc });
        cmd.Parameters.Add(new NpgsqlParameter("avg_type", NpgsqlDbType.Varchar) { Value = avgType });

        var results = new List<TelemetryAverage>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TelemetryAverage
            {
                DeviceId = reader.GetString(reader.GetOrdinal("device_id")),
                CalculatedDateTime = reader.GetDateTime(reader.GetOrdinal("calculateddatetime")),
                InsertedDateTime = reader.GetDateTime(reader.GetOrdinal("inserteddatetime")),
                AvgType = reader.GetString(reader.GetOrdinal("avgtype")),
                PM10Calibrated = ReadNullableDouble(reader, "pm10calibrated"),
                PM25Calibrated = ReadNullableDouble(reader, "pm25calibrated"),
                NO2Ugm3CalibratedFiltered = ReadNullableDouble(reader, "no2ugm3calibratedfiltered"),
                O3Ugm3CalibratedFiltered = ReadNullableDouble(reader, "o3ugm3calibratedfiltered"),
                SO2Ugm3CalibratedFiltered = ReadNullableDouble(reader, "so2ugm3calibratedfiltered"),
                COUgm3CalibratedFiltered = ReadNullableDouble(reader, "cougm3calibratedfiltered"),
                NO2PpbCalibratedFiltered = ReadNullableDouble(reader, "no2ppbcalibratedfiltered"),
                O3PpbCalibratedFiltered = ReadNullableDouble(reader, "o3ppbcalibratedfiltered"),
                SO2PpbCalibratedFiltered = ReadNullableDouble(reader, "so2ppbcalibratedfiltered"),
                COPpbCalibratedFiltered = ReadNullableDouble(reader, "coppbcalibratedfiltered"),
                VOCPpbRawFiltered = ReadNullableDouble(reader, "vocppbrawfiltered"),
                Humidity = ReadNullableDouble(reader, "humidity"),
                Temperature = ReadNullableDouble(reader, "temperature"),
                Pressure = ReadNullableDouble(reader, "pressure"),
                WindSpeed = ReadNullableDouble(reader, "windspeed"),
                WindDirectionFiltered = ReadNullableDouble(reader, "winddirectionfiltered"),
                NoiseCalibrated = ReadNullableDouble(reader, "noisecalibrated"),
                H2SPpbRawFiltered = ReadNullableDouble(reader, "h2sppbrawfiltered"),
                CO2Ppm = ReadNullableDouble(reader, "co2ppm")
            });
        }

        return results;
    }

    public async Task<Dictionary<string, double?>> GetTelemetryAverageStatsAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string pollutant,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default)
    {
        if (deviceIds == null || deviceIds.Count == 0)
        {
            return new Dictionary<string, double?>();
        }

        if (string.IsNullOrWhiteSpace(pollutant))
        {
            return new Dictionary<string, double?>();
        }

        // Map pollutant name to column name (WIDE FORMAT)
        var columnName = pollutant.ToUpperInvariant() switch
        {
            "PM10" => "pm10calibrated",
            "PM2.5" or "PM25" => "pm25calibrated",
            "NO2" => "no2ugm3calibratedfiltered",
            "O3" => "o3ugm3calibratedfiltered",
            "SO2" => "so2ugm3calibratedfiltered",
            "CO" => "cougm3calibratedfiltered",
            _ => null
        };

        if (columnName == null)
        {
            return new Dictionary<string, double?>();
        }

        startDateUtc = EnsureUtc(startDateUtc);
        endDateUtc = EnsureUtc(endDateUtc);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $@"
SELECT
    AVG({columnName}) AS avg_value,
    MIN({columnName}) AS min_value,
    MAX({columnName}) AS max_value,
    COUNT({columnName}) AS measurement_count
FROM telemetry_averages
WHERE device_id = ANY(@device_ids)
  AND calculateddatetime >= @start_ts
  AND calculateddatetime < @end_ts
  AND avgtype = @avg_type
  AND {columnName} IS NOT NULL
  AND {columnName}::text <> 'NaN';";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("device_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = deviceIds.ToArray()
        });
        cmd.Parameters.Add(new NpgsqlParameter("start_ts", NpgsqlDbType.TimestampTz) { Value = startDateUtc });
        cmd.Parameters.Add(new NpgsqlParameter("end_ts", NpgsqlDbType.TimestampTz) { Value = endDateUtc });
        cmd.Parameters.Add(new NpgsqlParameter("avg_type", NpgsqlDbType.Varchar) { Value = avgType });

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Dictionary<string, double?>
            {
                ["average"] = ReadNullableDouble(reader, "avg_value"),
                ["minimum"] = ReadNullableDouble(reader, "min_value"),
                ["maximum"] = ReadNullableDouble(reader, "max_value"),
                ["count"] = ReadNullableDouble(reader, "measurement_count")
            };
        }

        return new Dictionary<string, double?>();
    }

    public async Task<DateTime?> GetLatestTelemetryAverageTimestampAsync(
        IReadOnlyList<string> deviceIds,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default)
    {
        if (deviceIds == null || deviceIds.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(avgType))
        {
            avgType = "24h_rolling";
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = @"
SELECT MAX(calculateddatetime) AS max_ts
FROM telemetry_averages
WHERE device_id = ANY(@device_ids)
  AND avgtype = @avg_type;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("device_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = deviceIds.ToArray()
        });
        cmd.Parameters.Add(new NpgsqlParameter("avg_type", NpgsqlDbType.Varchar) { Value = avgType });

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null || result is DBNull)
        {
            return null;
        }

        if (result is DateTime dt)
        {
            return EnsureUtc(dt);
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, DateTime>> GetLatestTelemetryAverageTimestampsAsync(
        IReadOnlyList<string> deviceIds,
        IReadOnlyList<string> avgTypes,
        CancellationToken cancellationToken = default)
    {
        if (deviceIds == null || deviceIds.Count == 0)
        {
            return new Dictionary<string, DateTime>();
        }

        if (avgTypes == null || avgTypes.Count == 0)
        {
            avgTypes = new[] { "24h_rolling" };
        }

        var normalizedDeviceIds = deviceIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var normalizedAvgTypes = avgTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var cacheKey = $"pg:latest-telemetry:device:{normalizedDeviceIds.Length}:avg:{normalizedAvgTypes.Length}:" +
                       string.Join(',', normalizedAvgTypes) + ":" +
                       string.Join(',', normalizedDeviceIds.Take(25));

        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, DateTime>? cached) && cached is not null)
        {
            return cached;
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = @"
SELECT device_id, MAX(calculateddatetime) AS max_ts
FROM telemetry_averages
WHERE device_id = ANY(@device_ids)
  AND avgtype = ANY(@avg_types)
GROUP BY device_id;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("device_ids", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = normalizedDeviceIds
        });
        cmd.Parameters.Add(new NpgsqlParameter("avg_types", NpgsqlDbType.Array | NpgsqlDbType.Varchar)
        {
            Value = normalizedAvgTypes
        });

        var result = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var deviceId = reader.GetString(reader.GetOrdinal("device_id"));
            var ordinal = reader.GetOrdinal("max_ts");
            if (reader.IsDBNull(ordinal))
            {
                continue;
            }

            var dt = reader.GetDateTime(ordinal);
            result[deviceId] = EnsureUtc(dt);
        }

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(2)
        });

        return result;
    }

    private static double? ReadNullableDouble(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var val = reader.GetValue(ordinal);
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
