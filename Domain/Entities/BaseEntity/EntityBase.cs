using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities.BaseEntity
{
    public class EntityBase
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}
