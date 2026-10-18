using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using MediatR;
using System.Net;

namespace Application.Claims.Queries
{
    public sealed record GetClaimQuery(string Id) : IRequest<ResponseModel<Claim>>;

    public sealed class GetClaimQueryHandler(IClaimService claimService)
        : IRequestHandler<GetClaimQuery, ResponseModel<Claim>>
    {
        public async Task<ResponseModel<Claim>> Handle(GetClaimQuery request, CancellationToken cancellationToken)
        {
            var claim = await claimService.GetClaimAsync(request.Id);
            return claim is not null
                ? ResponseModel<Claim>.SuccessResult(claim)
                : ResponseModel<Claim>.FailureResult("Claim not found", HttpStatusCode.NotFound);
        }
    }
}