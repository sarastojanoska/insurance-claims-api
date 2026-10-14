using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Application.Services.CoverService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Net;

namespace Application.Covers.Commands
{
    public record UpdateCoverCommand(string Id, UpdateCoverDto Cover) : IRequest<ResponseModel<Cover>>;

    public class UpdateCoverCommandHandler(ICoverService coverService, IChannelQueue<AuditLog> channelQueue) : IRequestHandler<UpdateCoverCommand, ResponseModel<Cover>>
    {
        public async Task<ResponseModel<Cover>> Handle(UpdateCoverCommand request, CancellationToken cancellationToken)
        {
            var result = await coverService.UpdateCoverAsync(request.Id, request.Cover);

            if (!result.Success)
                return ResponseModel<Cover>.FailureResult(result.Error ?? "Cover not found or failed to update.");

            await channelQueue.EnqueueAsync(new AuditLog
            {
                EntityName = EntityNameEnum.Cover,
                EntityId = request.Id,
                Type = HttpRequestTypeEnum.PUT
            }, cancellationToken);

            return ResponseModel<Cover>.SuccessResult(null, statusCode: HttpStatusCode.Accepted);
        }
    }
}