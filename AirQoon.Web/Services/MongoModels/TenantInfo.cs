using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AirQoon.Web.Services.MongoModels;

[BsonIgnoreExtraElements]
public class TenantInfo
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("SlugName")]
    public string SlugName { get; set; } = string.Empty;

    [BsonElement("Name")]
    public string? Name { get; set; }

    [BsonElement("IsPublic")]
    public bool IsPublic { get; set; }
}
