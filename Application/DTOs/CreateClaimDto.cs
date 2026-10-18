using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public record CreateClaimDto(string CoverId, string Name, ClaimType Type, decimal DamageCost, [property: JsonRequired] DateTime Created);
}
