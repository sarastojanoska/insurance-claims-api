using Domain.Enums;

namespace Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public EntityNameEnum EntityName { get; set; }

        public string EntityId { get; set; } = string.Empty;

        public HttpRequestTypeEnum Type { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
