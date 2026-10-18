using Domain.Enums;
using Services.PremiumCalculationService;
using Xunit;

namespace Claims.Tests.UnitTests.Services
{
    public class PremiumCalculationServiceTests
    {
        private readonly PremiumCalculationService _sut = new();

        [Fact]
        public void ComputePremium_ThrowsArgumentException_WhenCoverTypeIsUndefined()
        {
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 2, 1);

            var exception = Assert.Throws<ArgumentException>(
                () => _sut.ComputePremium(startDate, endDate, (CoverType)999));

            Assert.Contains("defined value", exception.Message);
        }

        [Fact]
        public void ComputePremium_ThrowsArgumentException_WhenEndDateIsBeforeStartDate()
        {
            var startDate = new DateTime(2025, 2, 1);
            var endDate = new DateTime(2025, 1, 1);

            var exception = Assert.Throws<ArgumentException>(
                () => _sut.ComputePremium(startDate, endDate, CoverType.Yacht));

            Assert.Contains("End date cannot be earlier", exception.Message);
        }

        [Fact]
        public void ComputePremium_ReturnsZero_WhenStartAndEndDateAreTheSame()
        {
            var date = new DateTime(2025, 1, 1);

            var premium = _sut.ComputePremium(date, date, CoverType.Yacht);

            Assert.Equal(0m, premium);
        }

        [Theory]
        [InlineData(CoverType.Yacht, 1.10)]
        [InlineData(CoverType.PassengerShip, 1.20)]
        [InlineData(CoverType.Tanker, 1.50)]
        [InlineData(CoverType.ContainerShip, 1.30)]
        [InlineData(CoverType.BulkCarrier, 1.30)]
        public void ComputePremium_UsesFullRate_ForFirstThirtyDays(CoverType coverType, double multiplier)
        {
            var startDate = new DateTime(2025, 1, 1);
            var endDate = startDate.AddDays(30);

            var premium = _sut.ComputePremium(startDate, endDate, coverType);

            var expected = 30 * 1250m * (decimal)multiplier;
            Assert.Equal(expected, premium);
        }

        [Fact]
        public void ComputePremium_AppliesDiscount_ForSecondPeriod_Yacht()
        {
            var startDate = new DateTime(2025, 1, 1);
            var endDate = startDate.AddDays(60);

            var premium = _sut.ComputePremium(startDate, endDate, CoverType.Yacht);

            var dailyRate = 1250m * 1.10m;
            var expected = 30 * dailyRate + 30 * dailyRate * (1 - 0.05m);
            Assert.Equal(expected, premium);
        }

        [Fact]
        public void ComputePremium_AppliesDiscount_ForRemainingPeriod_NonYacht()
        {
            var startDate = new DateTime(2025, 1, 1);
            var endDate = startDate.AddDays(200);

            var premium = _sut.ComputePremium(startDate, endDate, CoverType.ContainerShip);

            var dailyRate = 1250m * 1.30m;
            var initial = 30 * dailyRate;
            var discounted = 150 * dailyRate * (1 - 0.02m);
            var remainingDays = 200 - 30 - 150;
            var remaining = remainingDays * dailyRate * (1 - 0.03m);

            Assert.Equal(initial + discounted + remaining, premium);
        }
    }
}
