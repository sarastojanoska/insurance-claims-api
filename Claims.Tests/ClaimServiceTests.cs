using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services.ClaimService;
using Xunit;

namespace Claims.Tests
{
    public class ClaimServiceTests
    {
        [Fact]
        public async Task Creation_persists_the_exact_timestamp_validated_against_the_cover()
        {
            var expectedTimestamp = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new AppDbContext(options);
            var cover = new Cover { StartDate = expectedTimestamp, EndDate = expectedTimestamp };
            context.Covers.Add(cover);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new ClaimService(context, NullLogger<ClaimService>.Instance);

            var response = await service.CreateClaimAsync(new CreateClaimDto(cover.Id, "claim", ClaimType.Fire, 100_000m, expectedTimestamp));

            Assert.True(response.Success, response.Error);
            Assert.NotNull(response.Data);
            Assert.Equal(expectedTimestamp, response.Data.DateCreated);
            var savedClaim = await context.Claims.AsNoTracking()
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(expectedTimestamp, savedClaim.DateCreated);
            Assert.Equal(DateTimeKind.Utc, savedClaim.DateCreated.Kind);
        }
    }
}