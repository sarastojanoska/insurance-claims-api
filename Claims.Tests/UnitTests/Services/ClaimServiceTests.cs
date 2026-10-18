using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services.ClaimService;
using Xunit;

namespace Claims.Tests.UnitTests.Services
{
    public class ClaimServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static ClaimService CreateSut(AppDbContext context)
            => new(context, NullLogger<ClaimService>.Instance);

        private static async Task<Cover> SeedCoverAsync(AppDbContext context)
        {
            var cover = new Cover
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                Type = CoverType.Yacht,
                Premium = 1000m
            };

            await context.Covers.AddAsync(cover);
            await context.SaveChangesAsync();

            return cover;
        }

        [Fact]
        public async Task CreateClaimAsync_ReturnsFailure_WhenCoverNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var dto = new CreateClaimDto("missing-cover", "Damage", ClaimType.Collision, 500m, new DateTime(2025, 2, 1));

            var result = await sut.CreateClaimAsync(dto);

            Assert.False(result.Success);
            Assert.Equal("Cover not found.", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(100_001)]
        public async Task CreateClaimAsync_ReturnsFailure_WhenDamageCostIsInvalid(decimal damageCost)
        {
            await using var context = CreateContext();
            var cover = await SeedCoverAsync(context);
            var sut = CreateSut(context);

            var dto = new CreateClaimDto(cover.Id, "Damage", ClaimType.Collision, damageCost, new DateTime(2025, 2, 1));

            var result = await sut.CreateClaimAsync(dto);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateClaimAsync_ReturnsFailure_WhenCreatedDateOutsideCoverPeriod()
        {
            await using var context = CreateContext();
            var cover = await SeedCoverAsync(context);
            var sut = CreateSut(context);

            var dto = new CreateClaimDto(cover.Id, "Damage", ClaimType.Collision, 500m, new DateTime(2026, 1, 1));

            var result = await sut.CreateClaimAsync(dto);

            Assert.False(result.Success);
            Assert.Equal("Claim creation date must be within the related cover period.", result.Error);
        }

        [Fact]
        public async Task CreateClaimAsync_ReturnsSuccess_WhenValid()
        {
            await using var context = CreateContext();
            var cover = await SeedCoverAsync(context);
            var sut = CreateSut(context);

            var createdDate = new DateTime(2025, 3, 1);
            var dto = new CreateClaimDto(cover.Id, "Hull damage", ClaimType.Collision, 500m, createdDate);

            var result = await sut.CreateClaimAsync(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(cover.Id, result.Data!.CoverId);
            Assert.Equal(createdDate, result.Data.DateCreated);
            Assert.Single(context.Claims);
        }

        [Fact]
        public async Task GetClaimAsync_ReturnsNull_WhenClaimNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var claim = await sut.GetClaimAsync("missing-id");

            Assert.Null(claim);
        }

        [Fact]
        public async Task GetClaimsAsync_ReturnsAllClaims()
        {
            await using var context = CreateContext();
            var cover = await SeedCoverAsync(context);

            context.Claims.AddRange(
                new Claim { CoverId = cover.Id, Name = "Claim1", Type = ClaimType.Fire, DamageCost = 100m },
                new Claim { CoverId = cover.Id, Name = "Claim2", Type = ClaimType.BadWeather, DamageCost = 200m });
            await context.SaveChangesAsync();

            var sut = CreateSut(context);

            var claims = await sut.GetClaimsAsync();

            Assert.Equal(2, claims.Count());
        }

        [Fact]
        public async Task UpdateClaimAsync_ReturnsFailure_WhenClaimNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var dto = new UpdateClaimDto("Updated", ClaimType.Fire, 500m);

            var result = await sut.UpdateClaimAsync("missing-id", dto);

            Assert.False(result.Success);
            Assert.Equal("Claim not found.", result.Error);
        }

        [Fact]
        public async Task UpdateClaimAsync_ReturnsSuccess_WhenValid()
        {
            await using var context = CreateContext();
            var cover = await SeedCoverAsync(context);

            var claim = new Claim
            {
                CoverId = cover.Id,
                Name = "Original",
                Type = ClaimType.Collision,
                DamageCost = 100m,
                DateCreated = new DateTime(2025, 3, 1)
            };
            context.Claims.Add(claim);
            await context.SaveChangesAsync();

            var sut = CreateSut(context);
            var dto = new UpdateClaimDto("Updated", ClaimType.Fire, 700m);

            var result = await sut.UpdateClaimAsync(claim.Id, dto);

            Assert.True(result.Success);
            Assert.Equal("Updated", result.Data!.Name);
            Assert.Equal(ClaimType.Fire, result.Data.Type);
            Assert.Equal(700m, result.Data.DamageCost);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsFailure_WhenClaimNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var result = await sut.DeleteClaimAsync("missing-id");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task DeleteClaimAsync_ReturnsSuccess_WhenClaimExists()
        {
            await using var context = CreateContext();
            var cover = await SeedCoverAsync(context);

            var claim = new Claim { CoverId = cover.Id, Name = "ToDelete", Type = ClaimType.Collision, DamageCost = 100m };
            context.Claims.Add(claim);
            await context.SaveChangesAsync();

            var sut = CreateSut(context);

            var result = await sut.DeleteClaimAsync(claim.Id);

            Assert.True(result.Success);
            Assert.Empty(context.Claims);
        }
    }
}
