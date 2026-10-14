using Application.Abstractions;
using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Claims.Commands;

public sealed record DeleteClaimCommand(string Id) : IRequest<ResponseModel<Claim>>;

public sealed class DeleteClaimCommandHandler(IClaimService claimService, IChannelQueue<AuditLog> channelQueue,
    ILogger<DeleteClaimCommandHandler> logger) : IRequestHandler<DeleteClaimCommand, ResponseModel<Claim>>
{
    public async Task<ResponseModel<Claim>> Handle(DeleteClaimCommand request, CancellationToken cancellationToken)
    {
        var result = await claimService.DeleteClaimAsync(request.Id);
        if (!result.Success || result.Data is null)
        {
            logger.LogWarning("Claim not found or delete operation failed.");
            return ResponseModel<Claim>.FailureResult(result.Error ?? "Failed to delete the claim");
        }

        await channelQueue.EnqueueAsync(new AuditLog
        {
            EntityName = EntityNameEnum.Claim,
            EntityId = result.Data.Id,
            Type = HttpRequestTypeEnum.DELETE
        }, cancellationToken);

        return ResponseModel<Claim>.SuccessResult(result.Data);
    }
}