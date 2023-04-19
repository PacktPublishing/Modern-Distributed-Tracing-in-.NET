using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace database;

public class Record
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Name")]
    public string? Name { get; set; }
}
