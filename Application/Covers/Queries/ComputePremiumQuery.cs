using Application.Models;
using Application.Services.PremiumCalculationService;
using Domain.Enums;
using MediatR;

namespace Application.Covers.Queries;

public record ComputePremiumQuery(DateTime StartDate, DateTime EndDate, CoverType CoverType) : IRequest<ResponseModel<decimal>>;

public class ComputePremiumQueryHandler(IPremiumCalculationService premiumCalculationService) : IRequestHandler<ComputePremiumQuery, ResponseModel<decimal>>
{
    public Task<ResponseModel<decimal>> Handle(ComputePremiumQuery request, CancellationToken cancellationToken)
    {
        var premium = premiumCalculationService.ComputePremium(request.StartDate, request.EndDate, request.CoverType);
        return Task.FromResult(ResponseModel<decimal>.SuccessResult(premium));
    }
}