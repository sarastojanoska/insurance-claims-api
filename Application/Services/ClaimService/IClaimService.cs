using Application.DTOs;
using Application.Models;
using Domain.Entities;

namespace Application.Services.ClaimService
{
    public interface IClaimService
    {
        Task<IEnumerable<Claim>> GetClaimsAsync();
        Task<Claim?> GetClaimAsync(string id);
        Task<ServiceResponseModel<Claim>> CreateClaimAsync(CreateClaimDto item);
        Task<ServiceResponseModel<Claim>> UpdateClaimAsync(string id, UpdateClaimDto item);
        Task<ServiceResponseModel<Claim>> DeleteClaimAsync(string id);
    }
}
