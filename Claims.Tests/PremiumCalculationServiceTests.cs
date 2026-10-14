using Domain.Enums;
using Services.PremiumCalculationService;
using Xunit;

namespace Claims.Tests
{
    public class PremiumCalculationServiceTests
    {
        private readonly PremiumCalculationService service = new();
        private static readonly DateTime StartDate = new(2026, 1, 1);

        public static TheoryData<CoverType, decimal, decimal, decimal, decimal> Rates => new()
        {
            { CoverType.Yacht, 1375m, 1306.25m, 1265m, 471212.50m },
            { CoverType.PassengerShip, 1500m, 1470m, 1455m, 534675m },
            { CoverType.ContainerShip, 1625m, 1592.50m, 1576.25m, 579231.25m },
            { CoverType.BulkCarrier, 1625m, 1592.50m, 1576.25m, 579231.25m },
            { CoverType.Tanker, 1875m, 1837.50m, 1818.75m, 668343.75m }
        };

        [Theory]
        [MemberData(nameof(Rates))]
        public void Premium_uses_progressive_rates_at_every_boundary(
            CoverType coverType, decimal initialRate, decimal discountedRate, decimal remainingRate, decimal yearlyPremium)
        {
            Assert.Equal(0m, Premium(0, coverType));
            Assert.Equal(initialRate, Premium(1, coverType));
            Assert.Equal(29 * initialRate, Premium(29, coverType));
            Assert.Equal(30 * initialRate, Premium(30, coverType));
            Assert.Equal(30 * initialRate + discountedRate, Premium(31, coverType));
            Assert.Equal(30 * initialRate + 149 * discountedRate, Premium(179, coverType));
            Assert.Equal(30 * initialRate + 150 * discountedRate, Premium(180, coverType));
            Assert.Equal(30 * initialRate + 150 * discountedRate + remainingRate, Premium(181, coverType));
            Assert.Equal(yearlyPremium, Premium(365, coverType));
        }

        [Fact]
        public void Reversed_calendar_dates_are_rejected()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                service.ComputePremium(StartDate, StartDate.AddDays(-1), CoverType.Yacht));

            Assert.Equal("endDate", exception.ParamName);
        }

        [Fact]
        public void Time_of_day_does_not_change_calendar_day_premium()
        {
            var premium = service.ComputePremium(
                StartDate.AddHours(23), StartDate.AddDays(31).AddHours(1), CoverType.Yacht);

            Assert.Equal(42556.25m, premium);
        }

        [Fact]
        public void Same_calendar_day_has_no_charge()
        {
            Assert.Equal(0m, service.ComputePremium(StartDate, StartDate.AddHours(23), CoverType.Yacht));
        }

        [Fact]
        public void Leap_day_counts_as_an_insured_day()
        {
            Assert.Equal(2750m, service.ComputePremium(new DateTime(2028, 2, 28), new DateTime(2028, 3, 1), CoverType.Yacht));
        }

        private decimal Premium(int days, CoverType coverType)
            => service.ComputePremium(StartDate, StartDate.AddDays(days), coverType);
    }
}