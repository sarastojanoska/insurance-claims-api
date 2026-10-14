using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Application.Services.CoverService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Net;

namespace Application.Covers.Commands;

public record CreateCoverCommand(CreateCoverDto Cover) : IRequest<ResponseModel<Cover>>;

public class CreateCoverCommandHandler(ICoverService coverService, IChannelQueue<AuditLog> channelQueue) : IRequestHandler<CreateCoverCommand, ResponseModel<Cover>>
{
    public async Task<ResponseModel<Cover>> Handle(CreateCoverCommand request, CancellationToken cancellationToken)
    {
        var result = await coverService.CreateCoverAsync(request.Cover);

        if (!result.Success || result.Data is null)
            return ResponseModel<Cover>.FailureResult(result.Error ?? "Failed to create cover");

        await channelQueue.EnqueueAsync(new AuditLog
        {
            EntityName = EntityNameEnum.Cover,
            EntityId = result.Data.Id,
            Type = HttpRequestTypeEnum.POST
        }, cancellationToken);

        return ResponseModel<Cover>.SuccessResult(result.Data, statusCode: HttpStatusCode.Created);
    }
}