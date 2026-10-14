using Application.DTOs;
using Application.Models;
using Domain.Entities;

namespace Application.Services.CoverService
{
    public interface ICoverService
    {
        Task<ServiceResponseModel<Cover>> CreateCoverAsync(CreateCoverDto cover);
        Task<Cover?> GetCoverAsync(string id);
        Task<IEnumerable<Cover>> GetCoversAsync();
        Task<ServiceResponseModel<Cover>> DeleteCoverAsync(string id);
        Task<ServiceResponseModel<Cover>> UpdateCoverAsync(string id, UpdateCoverDto item);
    }
}
