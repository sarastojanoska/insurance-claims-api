using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using MediatR;

namespace Application.Claims.Queries
{
    public sealed record GetClaimsQuery : IRequest<ResponseModel<IEnumerable<Claim>>>;

    public sealed class GetClaimsQueryHandler(IClaimService claimService)
        : IRequestHandler<GetClaimsQuery, ResponseModel<IEnumerable<Claim>>>
    {
        public async Task<ResponseModel<IEnumerable<Claim>>> Handle(GetClaimsQuery request, CancellationToken cancellationToken)
            => ResponseModel<IEnumerable<Claim>>.SuccessResult(await claimService.GetClaimsAsync());
    }
}