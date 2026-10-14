using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infrastructure.Configurations
{
    public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
    {
        public void Configure(EntityTypeBuilder<Claim> builder)
        {
            builder.ToCollection("claims");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.CoverId)
                .HasElementName("coverId")
                .IsRequired();

            builder.Property(c => c.Name)
                .HasElementName("name")
                .IsRequired();

            builder.Property(c => c.Type)
                .HasElementName("claimType")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(c => c.DamageCost)
                .HasElementName("damageCost")
                .IsRequired();

            builder.Property(c => c.DateCreated)
                .HasElementName("dateCreated")
                .IsRequired();
        }
    }
}
