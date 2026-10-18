using Infrastructure.Context.AppDbContext;
using Infrastructure.Context.AuditContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Claims.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public string AppDbName { get; } = Guid.NewGuid().ToString();
        public string AuditDbName { get; } = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<DbContextOptions<AuditContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AuditContext>>();

                services.AddDbContext<AuditContext>(options =>
                    options.UseInMemoryDatabase(AuditDbName));

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(AppDbName));
            });
        }
    }
}
