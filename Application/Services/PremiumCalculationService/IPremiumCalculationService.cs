using Domain.Enums;

namespace Application.Services.PremiumCalculationService
{
    public interface IPremiumCalculationService
    {
        decimal ComputePremium(
            DateTime startDate,
            DateTime endDate,
            CoverType coverType);
    }
}
