using Application.DTOs;
using Domain.Entities;

namespace Application.Services.ClaimService
{
    public interface IClaimService
    {
        Task<IEnumerable<Claim>> GetClaimsAsync();
        Task<Claim?> GetClaimAsync(string id);
        Task<Claim?> CreateClaimAsync(CreateClaimDto item);
        Task<int> UpdateClaimAsync(string id, UpdateClaimDto item);
        Task<Claim?> DeleteClaimAsync(string id);
    }
}
