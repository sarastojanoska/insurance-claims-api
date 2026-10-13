using Domain.Entities.BaseEntity;
using Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities;

public class Cover : EntityBase
{
    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

    [BsonElement("coverType")]
    public CoverType Type { get; set; }

    [BsonElement("premium")]
    public decimal Premium { get; set; }
}
