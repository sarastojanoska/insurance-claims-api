using Domain.Enums;

namespace Application.Services.PremiumCalculationService
{
    public interface IPremiumCalculationService
    {
        decimal ComputePremiumAsync(DateTime startDate, DateTime endDate, CoverType coverType);
    }
}
