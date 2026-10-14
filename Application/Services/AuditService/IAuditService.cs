using Domain.Entities;

namespace Application.Services.AuditService
{
    public interface IAuditService
    {
        Task<AuditLog> AddAuditLogAsync(AuditLog auditLog);
    }
}
