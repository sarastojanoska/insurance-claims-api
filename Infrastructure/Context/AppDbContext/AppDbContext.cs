
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infastructure.Context.AppDbContext;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Cover> Covers => Set<Cover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>().ToCollection("claims");
        modelBuilder.Entity<Cover>().ToCollection("covers");

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(x => x.CoverId)
                .HasElementName("coverId")
                .IsRequired();

            entity.Property(x => x.Name)
                .HasElementName("name")
                .IsRequired();

            entity.Property(x => x.Type)
                .HasElementName("claimType")
                .HasConversion<string>()
                .IsRequired();

            entity.Property(x => x.DamageCost)
                .HasElementName("damageCost")
                .IsRequired();
        });

        modelBuilder.Entity<Cover>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(x => x.StartDate)
                .HasElementName("startDate");

            entity.Property(x => x.EndDate)
                .HasElementName("endDate");

            entity.Property(x => x.Type)
                .HasElementName("coverType")
                .HasConversion<string>();

            entity.Property(x => x.Premium)
                .HasElementName("premium");
        });
    }
}
