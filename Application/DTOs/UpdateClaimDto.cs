using Domain.Enums;

namespace Application.DTOs
{
    public record UpdateClaimDto(string Name, ClaimType Type, decimal DamageCost);
}
