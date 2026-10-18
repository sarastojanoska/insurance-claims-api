using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services.CoverService;
using Services.PremiumCalculationService;
using Xunit;

namespace Claims.Tests.UnitTests.Services
{
    public class CoverServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static CoverService CreateSut(AppDbContext context)
            => new(context, new PremiumCalculationService(), NullLogger<CoverService>.Instance);

        [Fact]
        public async Task CreateCoverAsync_ReturnsFailure_WhenTypeIsUndefined()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var dto = new CreateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(1), (CoverType)999);

            var result = await sut.CreateCoverAsync(dto);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateCoverAsync_ReturnsFailure_WhenStartDateIsInThePast()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var dto = new CreateCoverDto(DateTime.Today.AddDays(-1), DateTime.Today.AddMonths(1), CoverType.Yacht);

            var result = await sut.CreateCoverAsync(dto);

            Assert.False(result.Success);
            Assert.Equal("StartDate cannot be in the past.", result.Error);
        }

        [Fact]
        public async Task CreateCoverAsync_ReturnsFailure_WhenEndDateBeforeStartDate()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var start = DateTime.Today.AddDays(10);
            var dto = new CreateCoverDto(start, start.AddDays(-1), CoverType.Yacht);

            var result = await sut.CreateCoverAsync(dto);

            Assert.False(result.Success);
            Assert.Equal("EndDate cannot be earlier than StartDate.", result.Error);
        }

        [Fact]
        public async Task CreateCoverAsync_ReturnsFailure_WhenPeriodExceedsOneYear()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var start = DateTime.Today.AddDays(1);
            var dto = new CreateCoverDto(start, start.AddYears(1).AddDays(1), CoverType.Yacht);

            var result = await sut.CreateCoverAsync(dto);

            Assert.False(result.Success);
            Assert.Equal("The insurance period cannot exceed one year.", result.Error);
        }

        [Fact]
        public async Task CreateCoverAsync_ReturnsSuccess_WhenValid()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var start = DateTime.Today.AddDays(1);
            var end = start.AddMonths(1);
            var dto = new CreateCoverDto(start, end, CoverType.Yacht);

            var result = await sut.CreateCoverAsync(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data!.Premium > 0);
            Assert.Single(context.Covers);
        }

        [Fact]
        public async Task GetCoverAsync_ReturnsNull_WhenNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var cover = await sut.GetCoverAsync("missing-id");

            Assert.Null(cover);
        }

        [Fact]
        public async Task GetCoversAsync_ReturnsAllCovers()
        {
            await using var context = CreateContext();
            context.Covers.AddRange(
                new Cover { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(1), Type = CoverType.Yacht, Premium = 100m },
                new Cover { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(2), Type = CoverType.Tanker, Premium = 200m });
            await context.SaveChangesAsync();

            var sut = CreateSut(context);

            var covers = await sut.GetCoversAsync();

            Assert.Equal(2, covers.Count());
        }

        [Fact]
        public async Task UpdateCoverAsync_ReturnsFailure_WhenCoverNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var dto = new UpdateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(1), CoverType.Yacht);

            var result = await sut.UpdateCoverAsync("missing-id", dto);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task UpdateCoverAsync_ReturnsSuccess_WhenValid()
        {
            await using var context = CreateContext();
            var cover = new Cover { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(1), Type = CoverType.Yacht, Premium = 100m };
            context.Covers.Add(cover);
            await context.SaveChangesAsync();

            var sut = CreateSut(context);
            var newStart = DateTime.Today.AddDays(1);
            var newEnd = newStart.AddMonths(2);
            var dto = new UpdateCoverDto(newStart, newEnd, CoverType.Tanker);

            var result = await sut.UpdateCoverAsync(cover.Id, dto);

            Assert.True(result.Success);
            Assert.Equal(CoverType.Tanker, result.Data!.Type);
            Assert.True(result.Data.Premium > 0);
        }

        [Fact]
        public async Task DeleteCoverAsync_ReturnsFailure_WhenCoverNotFound()
        {
            await using var context = CreateContext();
            var sut = CreateSut(context);

            var result = await sut.DeleteCoverAsync("missing-id");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task DeleteCoverAsync_ReturnsSuccess_WhenCoverExists()
        {
            await using var context = CreateContext();
            var cover = new Cover { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(1), Type = CoverType.Yacht, Premium = 100m };
            context.Covers.Add(cover);
            await context.SaveChangesAsync();

            var sut = CreateSut(context);

            var result = await sut.DeleteCoverAsync(cover.Id);

            Assert.True(result.Success);
            Assert.Empty(context.Covers);
        }
    }
}
