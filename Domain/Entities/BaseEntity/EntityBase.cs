
namespace Domain.Entities.BaseEntity
{
    public class EntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}
