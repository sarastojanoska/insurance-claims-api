using Application.DTOs;
using Application.Models;
using Application.Services.CoverService;
using Application.Services.PremiumCalculationService;
using Domain.Entities;
using Infrastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Services.CoverService
{
    public class CoverService(AppDbContext context, IPremiumCalculationService premiumCalculationService, ILogger<CoverService> logger) : ICoverService
    {
        public async Task<ServiceResponseModel<Cover>> CreateCoverAsync(CreateCoverDto cover)
        {
            var validationError = ValidateCoverDates(
                cover.StartDate,
                cover.EndDate);

            if (validationError is not null)
            {
                logger.LogWarning("Cover creation failed: {Error}", validationError);
                return ServiceResponseModel<Cover>.FailureResult(validationError);
            }

            var newCover = new Cover
            {
                StartDate = cover.StartDate,
                EndDate = cover.EndDate,
                Type = cover.Type,
                Premium = premiumCalculationService.ComputePremium(cover.StartDate, cover.EndDate, cover.Type)
            };

            await context.Covers.AddAsync(newCover);
            await context.SaveChangesAsync();

            return ServiceResponseModel<Cover>.SuccessResult(newCover);
        }

        public async Task<Cover?> GetCoverAsync(string id)
        {
            var cover = await context.Covers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (cover == null)
            {
                logger.LogError("Cover not found.");
                return null;
            }

            return cover;
        }

        public async Task<ServiceResponseModel<Cover>> UpdateCoverAsync(string id, UpdateCoverDto item)
        {
            var cover = await context.Covers.FirstOrDefaultAsync(c => c.Id == id);

            if (cover == null)
            {
                logger.LogWarning("Cover {CoverId} not found.", id);
                return ServiceResponseModel<Cover>.FailureResult("Cover not found.");
            }

            var validationError = ValidateCoverDates(item.StartDate, item.EndDate);

            if (validationError is not null)
            {
                logger.LogWarning("Cover update failed for {CoverId}: {Error}", id, validationError);
                return ServiceResponseModel<Cover>.FailureResult(validationError);
            }

            cover.StartDate = item.StartDate;
            cover.EndDate = item.EndDate;
            cover.Type = item.Type;
            cover.Premium = premiumCalculationService.ComputePremium(item.StartDate, item.EndDate, item.Type);

            await context.SaveChangesAsync();

            return ServiceResponseModel<Cover>.SuccessResult(cover);
        }

        public async Task<IEnumerable<Cover>> GetCoversAsync() => await context.Covers.AsNoTracking().ToListAsync();

        public async Task<ServiceResponseModel<Cover>> DeleteCoverAsync(string id)
        {
            var cover = await context.Covers.FirstOrDefaultAsync(x => x.Id == id);

            if (cover == null)
            {
                logger.LogWarning("Cover {CoverId} not found.", id);
                return ServiceResponseModel<Cover>.FailureResult("Cover not found.");
            }

            context.Covers.Remove(cover);
            await context.SaveChangesAsync();

            return ServiceResponseModel<Cover>.SuccessResult(cover);
        }

        private static string? ValidateCoverDates(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date < DateTime.Today)
            {
                return "StartDate cannot be in the past.";
            }

            if (endDate < startDate)
            {
                return "EndDate cannot be earlier than StartDate.";
            }

            if (endDate > startDate.AddYears(1))
            {
                return "The insurance period cannot exceed one year.";
            }

            return null;
        }
    }
}
