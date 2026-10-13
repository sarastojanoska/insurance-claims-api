using Application.Abstractions;
using Application.Models;
using Application.Services.CoverService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Covers.Commands;

public sealed record DeleteCoverCommand(string Id) : IRequest<ResponseModel<Cover>>;

public sealed class DeleteCoverCommandHandler(ICoverService coverService, IChannelQueue<AuditLog> channelQueue,
    ILogger<DeleteCoverCommandHandler> logger) : IRequestHandler<DeleteCoverCommand, ResponseModel<Cover>>
{
    public async Task<ResponseModel<Cover>> Handle(DeleteCoverCommand request, CancellationToken cancellationToken)
    {
        var result = await coverService.DeleteCoverAsync(request.Id);
        if (!result.Success || result.Data is null)
        {
            logger.LogError("Cover not found or delete operation failed.");
            return ResponseModel<Cover>.FailureResult(result.Error ?? "Failed to delete cover");
        }

        await channelQueue.EnqueueAsync(new AuditLog
        {
            EntityName = EntityNameEnum.Cover,
            EntityId = result.Data.Id,
            Type = HttpRequestTypeEnum.DELETE
        }, cancellationToken);

        return ResponseModel<Cover>.SuccessResult(result.Data);
    }
}