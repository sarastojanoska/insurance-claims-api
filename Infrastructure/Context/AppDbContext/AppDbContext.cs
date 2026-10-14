using Domain.Entities;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context.AppDbContext
{
    public class AppDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Claim> Claims => Set<Claim>();
        public DbSet<Cover> Covers => Set<Cover>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ClaimConfiguration());
            modelBuilder.ApplyConfiguration(new CoverConfiguration());
        }
    }
}
