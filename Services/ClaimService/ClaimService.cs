using Application.DTOs;
using Application.Services.ClaimService;
using Domain.Entities;
using Infastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Services.ClaimService
{
    public class ClaimService(AppDbContext context, ILogger<ClaimService> logger) : IClaimService
    {
        public async Task<IEnumerable<Claim>> GetClaimsAsync() => context.Claims.AsNoTracking().AsEnumerable();

        public async Task<Claim?> GetClaimAsync(string id)
        {
            var claim = await context.Claims.FirstOrDefaultAsync(x => x.Id == id);

            if (claim == null)
            {
                logger.LogError("Claim not found");
                return null;
            }

            return claim;
        }

        public async Task<Claim?> CreateClaimAsync(CreateClaimDto item)
        {
            var cover = await context.Covers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == item.CoverId);

            if (cover == null)
            {
                logger.LogError("Cover not found.");
                return null;
            }

            var claim = new Claim
            {
                CoverId = cover.Id,
                Name = item.Name,
                Type = item.Type,
                DamageCost = item.DamageCost
            };

            var result = await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            return result?.Entity;
        }

        public async Task<Claim?> DeleteClaimAsync(string id)
        {
            var claim = await context.Claims.FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                logger.LogError("Claim not found.");
                return null;
            }

            var result = context.Claims.Remove(claim);
            await context.SaveChangesAsync();

            return result?.Entity;
        }

        public async Task<int> UpdateClaimAsync(string id, UpdateClaimDto item)
        {
            var claim = await context.Claims.FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                logger.LogError("Claim not found.");
                return 0;
            }

            claim.Name = item.Name;
            claim.Type = item.Type;
            claim.DamageCost = item.DamageCost;

            var result = context.Claims.Update(claim);
            await context.SaveChangesAsync();

            if (result == null)
            {
                return 0;
            }

            return 1;
        }
    }
}
