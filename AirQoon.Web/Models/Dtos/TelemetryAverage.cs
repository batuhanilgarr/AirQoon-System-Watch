namespace AirQoon.Web.Models.Dtos;

/// <summary>
/// Telemetry averages from PostgreSQL telemetry_averages table (WIDE FORMAT).
/// Each row contains all pollutant averages for a device at a specific time.
/// </summary>
public class TelemetryAverage
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime CalculatedDateTime { get; set; }
    public DateTime InsertedDateTime { get; set; }
    public string AvgType { get; set; } = string.Empty; // '1h', '24h_rolling', '8h_rolling'
    
    // Calibrated pollutant values (averages) - each pollutant has its own column
    public double? PM10Calibrated { get; set; }
    public double? PM25Calibrated { get; set; }
    public double? NO2Ugm3CalibratedFiltered { get; set; }
    public double? O3Ugm3CalibratedFiltered { get; set; }
    public double? SO2Ugm3CalibratedFiltered { get; set; }
    public double? COUgm3CalibratedFiltered { get; set; }
    public double? NO2PpbCalibratedFiltered { get; set; }
    public double? O3PpbCalibratedFiltered { get; set; }
    public double? SO2PpbCalibratedFiltered { get; set; }
    public double? COPpbCalibratedFiltered { get; set; }
    public double? VOCPpbRawFiltered { get; set; }
    
    // Environmental data
    public double? Humidity { get; set; }
    public double? Temperature { get; set; }
    public double? Pressure { get; set; }
    public double? WindSpeed { get; set; }
    public double? WindDirectionFiltered { get; set; }
    public double? NoiseCalibrated { get; set; }
    public double? H2SPpbRawFiltered { get; set; }
    public double? CO2Ppm { get; set; }
}
