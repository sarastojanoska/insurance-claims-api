using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Net;

namespace Application.Claims.Commands;

public sealed record UpdateClaimCommand(string Id, UpdateClaimDto Claim) : IRequest<ResponseModel<Claim>>;

public sealed class UpdateClaimCommandHandler(IClaimService claimService, IChannelQueue<AuditLog> channelQueue)
    : IRequestHandler<UpdateClaimCommand, ResponseModel<Claim>>
{
    public async Task<ResponseModel<Claim>> Handle(UpdateClaimCommand request, CancellationToken cancellationToken)
    {
        var result = await claimService.UpdateClaimAsync(request.Id, request.Claim);
        if (!result.Success)
            return ResponseModel<Claim>.FailureResult(result.Error ?? "Claim not found or failed to update");

        await channelQueue.EnqueueAsync(new AuditLog
        {
            EntityName = EntityNameEnum.Claim,
            EntityId = request.Id,
            Type = HttpRequestTypeEnum.PUT
        }, cancellationToken);

        return ResponseModel<Claim>.SuccessResult(null, statusCode: HttpStatusCode.Accepted);
    }
}