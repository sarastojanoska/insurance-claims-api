using Application.Services.AuditService;
using Domain.Entities;
using Infrastructure.Context.AuditContext;

namespace Services.AuditService
{
    public class AuditService(AuditContext context) : IAuditService
    {
        public async Task<AuditLog> AddAuditLogAsync(AuditLog auditLog)
        {
            var result = await context.AuditLogs.AddAsync(auditLog);
            await context.SaveChangesAsync();

            return result.Entity;
        }
    }
}
