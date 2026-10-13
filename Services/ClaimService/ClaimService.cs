using Application.DTOs;
using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using Infastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Services.ClaimService
{
    public class ClaimService(AppDbContext context, ILogger<ClaimService> logger, TimeProvider? timeProvider = null) : IClaimService
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



        public async Task<ServiceResponseModel<Claim>> CreateClaimAsync(
            CreateClaimDto item)
        {
            var cover = await context.Covers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == item.CoverId);

            if (cover == null)
            {
                logger.LogWarning("Cover {CoverId} not found.", item.CoverId);

                return ServiceResponseModel<Claim>.FailureResult(
                    "Cover not found.");
            }

            var createdDate = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

            var validationError = ValidateClaim(
                item.DamageCost,
                createdDate,
                cover.StartDate,
                cover.EndDate);

            if (validationError is not null)
            {
                logger.LogWarning(
                    "Claim creation failed: {Error}",
                    validationError);

                return ServiceResponseModel<Claim>.FailureResult(
                    validationError);
            }

            var claim = new Claim
            {
                CoverId = cover.Id,
                Name = item.Name,
                Type = item.Type,
                DamageCost = item.DamageCost,
                DateCreated = createdDate
            };

            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            return ServiceResponseModel<Claim>.SuccessResult(claim);
        }


        public async Task<ServiceResponseModel<Claim>> DeleteClaimAsync(string id)
        {
            var claim = await context.Claims.FirstOrDefaultAsync(c => c.Id == id);

            if (claim is null)
            {
                logger.LogWarning("Cannot delete claim {ClaimId}: claim was not found.", id);

                return ServiceResponseModel<Claim>.FailureResult(
                    $"Claim with ID '{id}' was not found.");
            }
            var result = context.Claims.Remove(claim);
            await context.SaveChangesAsync();

            logger.LogInformation("Claim {ClaimId} deleted successfully.", id);

            return ServiceResponseModel<Claim>.SuccessResult(claim);
        }


        public async Task<ServiceResponseModel<Claim>> UpdateClaimAsync(
            string id,
            UpdateClaimDto item)
        {
            var claim = await context.Claims
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                logger.LogWarning("Claim {ClaimId} not found.", id);

                return ServiceResponseModel<Claim>.FailureResult(
                    "Claim not found.");
            }

            var cover = await context.Covers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == claim.CoverId);

            if (cover == null)
            {
                logger.LogWarning(
                    "Cover {CoverId} associated with claim {ClaimId} not found.",
                    claim.CoverId,
                    id);

                return ServiceResponseModel<Claim>.FailureResult(
                    "Related cover not found.");
            }

            var validationError = ValidateClaim(
                item.DamageCost,
                claim.DateCreated,
                cover.StartDate,
                cover.EndDate);

            if (validationError is not null)
            {
                logger.LogWarning(
                    "Claim update failed for {ClaimId}: {Error}",
                    id,
                    validationError);

                return ServiceResponseModel<Claim>.FailureResult(
                    validationError);
            }

            claim.Name = item.Name;
            claim.Type = item.Type;
            claim.DamageCost = item.DamageCost;

            await context.SaveChangesAsync();

            return ServiceResponseModel<Claim>.SuccessResult(claim);
        }


        private static string? ValidateClaim(
            decimal damageCost,
            DateTime createdDate,
            DateTime coverStartDate,
            DateTime coverEndDate)
        {
            if (damageCost > 100_000)
            {
                return "DamageCost cannot exceed 100,000.";
            }

            if (createdDate < coverStartDate || createdDate > coverEndDate)
            {
                return "Claim creation date must be within the related cover period.";
            }

            return null;
        }

    }
}
