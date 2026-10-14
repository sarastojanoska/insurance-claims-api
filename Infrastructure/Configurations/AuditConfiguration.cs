using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AuditConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(e => e.EntityName)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e => e.EntityId)
                .IsRequired();

            builder.Property(e => e.Type)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(e => new { e.EntityName, e.EntityId });
            builder.HasIndex(e => e.Timestamp);
        }
    }
}
