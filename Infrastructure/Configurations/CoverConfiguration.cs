using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infrastructure.Configurations
{
    public class CoverConfiguration : IEntityTypeConfiguration<Cover>
    {
        public void Configure(EntityTypeBuilder<Cover> builder)
        {
            builder.ToCollection("covers");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.StartDate)
                .HasElementName("startDate")
                .IsRequired();

            builder.Property(c => c.EndDate)
                .HasElementName("endDate")
                .IsRequired();

            builder.Property(c => c.Type)
                .HasElementName("coverType")
                .HasConversion<string>()
                .IsRequired();

            builder.Property(c => c.Premium)
                .HasElementName("premium")
                .IsRequired();

            builder.Property(c => c.DateCreated)
                .HasElementName("dateCreated")
                .IsRequired();
        }
    }
}
