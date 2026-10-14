using Application.Services.PremiumCalculationService;
using Domain.Enums;

namespace Services.PremiumCalculationService
{
    public class PremiumCalculationService : IPremiumCalculationService
    {
        private const decimal BaseDailyRate = 1250m;
        private const int InitialPeriodDays = 30;
        private const int DiscountedPeriodDays = 150;

        public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
        {
            var insuranceDays = (endDate.Date - startDate.Date).Days;

            if (insuranceDays < 0)
            {
                throw new ArgumentException("End date cannot be earlier than start date.", nameof(endDate));
            }

            var dailyRate = BaseDailyRate * GetTypeMultiplier(coverType);

            var firstPeriodDays = Math.Min(insuranceDays, InitialPeriodDays);

            var secondPeriodDays = Math.Min(Math.Max(insuranceDays - InitialPeriodDays, 0), DiscountedPeriodDays);
            var remainingDays = Math.Max(insuranceDays - InitialPeriodDays - DiscountedPeriodDays, 0);

            var secondPeriodDiscount = coverType == CoverType.Yacht ? 0.05m : 0.02m;
            var additionalDiscount = coverType == CoverType.Yacht ? 0.03m : 0.01m;

            var remainingPeriodDiscount = secondPeriodDiscount + additionalDiscount;

            var initialPremium = firstPeriodDays * dailyRate;
            var discountedPremium = secondPeriodDays * dailyRate * (1 - secondPeriodDiscount);
            var remainingPremium = remainingDays * dailyRate * (1 - remainingPeriodDiscount);

            return initialPremium + discountedPremium + remainingPremium;
        }

        private static decimal GetTypeMultiplier(CoverType coverType)
        {
            return coverType switch
            {
                CoverType.Yacht => 1.10m,
                CoverType.PassengerShip => 1.20m,
                CoverType.Tanker => 1.50m,
                _ => 1.30m
            };
        }
    }
}
