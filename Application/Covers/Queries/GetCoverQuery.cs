using Application.Models;
using Application.Services.CoverService;
using Domain.Entities;
using MediatR;
using System.Net;

namespace Application.Covers.Queries;

public sealed record GetCoverQuery(string Id) : IRequest<ResponseModel<Cover>>;

public sealed class GetCoverQueryHandler(ICoverService coverService)
    : IRequestHandler<GetCoverQuery, ResponseModel<Cover>>
{
    public async Task<ResponseModel<Cover>> Handle(GetCoverQuery request, CancellationToken cancellationToken)
    {
        var cover = await coverService.GetCoverAsync(request.Id);
        return cover is not null
            ? ResponseModel<Cover>.SuccessResult(cover)
            : ResponseModel<Cover>.FailureResult("Not found", HttpStatusCode.NotFound);
    }
}