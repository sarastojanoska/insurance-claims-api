using Application.DTOs;
using Application.Services.CoverService;
using Application.Services.PremiumCalculationService;
using Domain.Entities;
using Infastructure.Context.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Services.CoverService
{
    public class CoverService(AppDbContext context, IPremiumCalculationService premiumCalculationService, ILogger<CoverService> logger) : ICoverService
    {
        public async Task<Cover> CreateCoverAsync(CreateCoverDto cover)
        {
            var newCover = new Cover
            {
                StartDate = cover.StartDate,
                EndDate = cover.EndDate,
                Type = cover.Type,
                Premium = premiumCalculationService.ComputePremiumAsync(cover.StartDate, cover.EndDate, cover.Type)
            };

            var result = await context.Covers.AddAsync(newCover);
            await context.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<Cover?> GetCoverAsync(string id)
        {
            var cover = await context.Covers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

            if (cover == null)
            {
                logger.LogError("Cover not found.");
                return null;
            }

            return cover;
        }

        public async Task<int> UpdateCoverAsync(string id, UpdateCoverDto item)
        {
            var cover = await context.Covers.FirstOrDefaultAsync(c => c.Id == id);

            if (cover == null)
            {
                logger.LogError("Cover not found.");
                return 0;
            }

            cover.StartDate = item.StartDate;   // TODO: Validation, StartDate cannot be greater than EndDate
            cover.EndDate = item.EndDate;
            cover.Type = item.Type;

            // recalculate premium based on latest data
            cover.Premium = premiumCalculationService.ComputePremiumAsync(item.StartDate, item.EndDate, item.Type);

            var result = context.Covers.Update(cover);
            await context.SaveChangesAsync();

            return 1;
        }

        public async Task<IEnumerable<Cover>> GetCoversAsync() => context.Covers.AsNoTracking().AsEnumerable();

        public async Task<Cover?> DeleteCoverAsync(string id)
        {
            var cover = await context.Covers.FirstOrDefaultAsync(x => x.Id == id);

            if (cover == null)
            {
                logger.LogError("Cover not found.");
                return null;
            }

            var result = context.Covers.Remove(cover).Entity;
            await context.SaveChangesAsync();

            return result;
        }
    }
}
