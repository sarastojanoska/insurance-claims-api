using Application.DTOs;
using Domain.Entities;

namespace Application.Services.CoverService
{
    public interface ICoverService
    {
        Task<Cover> CreateCoverAsync(CreateCoverDto cover);
        Task<Cover?> GetCoverAsync(string id);
        Task<IEnumerable<Cover>> GetCoversAsync();
        Task<Cover?> DeleteCoverAsync(string id);
        Task<int> UpdateCoverAsync(string id, UpdateCoverDto item);
    }
}
