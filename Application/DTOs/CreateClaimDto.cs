using Domain.Enums;

namespace Application.DTOs
{
    public record CreateClaimDto(string CoverId, string Name, ClaimType Type, decimal DamageCost);
}
