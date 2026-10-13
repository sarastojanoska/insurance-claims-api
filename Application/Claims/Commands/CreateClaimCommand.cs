using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Net;

namespace Application.Claims.Commands;

public sealed record CreateClaimCommand(CreateClaimDto Claim) : IRequest<ResponseModel<Claim>>;

public sealed class CreateClaimCommandHandler(IClaimService claimService, IChannelQueue<AuditLog> channelQueue)
    : IRequestHandler<CreateClaimCommand, ResponseModel<Claim>>
{
    public async Task<ResponseModel<Claim>> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var result = await claimService.CreateClaimAsync(request.Claim);
        if (!result.Success || result.Data is null)
            return ResponseModel<Claim>.FailureResult(result.Error ?? "Failed to create claim");

        await channelQueue.EnqueueAsync(new AuditLog
        {
            EntityName = EntityNameEnum.Claim,
            EntityId = result.Data.Id,
            Type = HttpRequestTypeEnum.POST
        }, cancellationToken);

        return ResponseModel<Claim>.SuccessResult(result.Data, statusCode: HttpStatusCode.Created);
    }
}