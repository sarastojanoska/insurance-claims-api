using Application.Models;
using Application.Services.CoverService;
using Domain.Entities;
using MediatR;

namespace Application.Covers.Queries
{
    public record GetCoversQuery : IRequest<ResponseModel<IEnumerable<Cover>>>;

    public class GetCoversQueryHandler(ICoverService coverService) : IRequestHandler<GetCoversQuery, ResponseModel<IEnumerable<Cover>>>
    {
        public async Task<ResponseModel<IEnumerable<Cover>>> Handle(GetCoversQuery request, CancellationToken cancellationToken)
            => ResponseModel<IEnumerable<Cover>>.SuccessResult(await coverService.GetCoversAsync());
    }
}