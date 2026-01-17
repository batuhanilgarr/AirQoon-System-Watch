using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AirQoon.Web.Services.MongoModels;

[BsonIgnoreExtraElements]
public class DeviceInfoRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [BsonElement("TenantSlugName")]
    public string TenantSlugName { get; set; } = string.Empty;

    [BsonElement("Name")]
    public string? Name { get; set; }

    [BsonElement("Label")]
    public string? Label { get; set; }

    [BsonElement("LatestTelemetry")]
    public object? LatestTelemetry { get; set; }
}
