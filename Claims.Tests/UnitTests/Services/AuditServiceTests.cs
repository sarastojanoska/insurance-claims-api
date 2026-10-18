using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context.AuditContext;
using Microsoft.EntityFrameworkCore;
using Services.AuditService;
using Xunit;

namespace Claims.Tests.UnitTests.Services
{
    public class AuditServiceTests
    {
        private static AuditContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AuditContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AuditContext(options);
        }

        [Fact]
        public async Task AddAuditLogAsync_PersistsAuditLog_AndReturnsIt()
        {
            await using var context = CreateContext();
            var sut = new AuditService(context);

            var auditLog = new AuditLog
            {
                EntityName = EntityNameEnum.Claim,
                EntityId = "claim-1",
                Type = HttpRequestTypeEnum.POST
            };

            var result = await sut.AddAuditLogAsync(auditLog);

            Assert.Equal(auditLog.Id, result.Id);
            Assert.Single(context.AuditLogs);
            Assert.Equal("claim-1", context.AuditLogs.Single().EntityId);
        }

        [Fact]
        public async Task AddAuditLogAsync_PersistsMultipleAuditLogs()
        {
            await using var context = CreateContext();
            var sut = new AuditService(context);

            await sut.AddAuditLogAsync(new AuditLog { EntityName = EntityNameEnum.Cover, EntityId = "cover-1", Type = HttpRequestTypeEnum.POST });
            await sut.AddAuditLogAsync(new AuditLog { EntityName = EntityNameEnum.Cover, EntityId = "cover-1", Type = HttpRequestTypeEnum.PUT });

            Assert.Equal(2, context.AuditLogs.Count());
        }
    }
}
