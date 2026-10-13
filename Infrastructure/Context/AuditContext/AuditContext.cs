using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infastructure.Context.AuditContext
{
    public class AuditContext(DbContextOptions<AuditContext> options) 
        : DbContext(options)
    {
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuditLog>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<AuditLog>()
                .Property(e => e.EntityId)
                .IsRequired(true);
        }
    }
}
