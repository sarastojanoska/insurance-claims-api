using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Application.Services.CoverService;
using Application.Services.PremiumCalculationService;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoversController(ICoverService coverService,
                              IPremiumCalculationService premiumCalculationService,
                              IChannelQueue<AuditLog> channelQueue,
                              ILogger<CoversController> logger) : ControllerBase
{
    [HttpPost("compute")]
    public async Task<ActionResult> ComputePremiumAsync(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return Ok(premiumCalculationService.ComputePremiumAsync(startDate, endDate, coverType));
    }

    [HttpGet]
    public async Task<ActionResult<ResponseModel<IEnumerable<Cover>>>> GetAsync()
    {
        var data = await coverService.GetCoversAsync();
        return ResponseModel<IEnumerable<Cover>>.SuccessResult(data);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseModel<Cover>>> GetAsync(string id)
    {
        var result = await coverService.GetCoverAsync(id);

        if (result != null)
            return ResponseModel<Cover>.SuccessResult(result);

        return ResponseModel<Cover>.FailureResult("Not found", HttpStatusCode.NotFound);
    }

    [HttpPost]
    public async Task<ActionResult<ResponseModel<Cover>>> CreateAsync(CreateCoverDto coverDto, CancellationToken cancellationToken)
    {
        var result = await coverService.CreateCoverAsync(coverDto);

        if (result != null)
        {
            var auditLog = new AuditLog
            {
                EntityName = EntityNameEnum.Cover,
                EntityId = result.Id,
                Type = HttpRequestTypeEnum.POST,
            };

            await channelQueue.EnqueueAsync(auditLog, cancellationToken);
            return ResponseModel<Cover>.SuccessResult(result, statusCode: HttpStatusCode.Created);
        }

        return ResponseModel<Cover>.FailureResult("Failed to create cover");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseModel<Cover>>> UpdateAsync(string id, [FromBody] UpdateCoverDto coverDto)
    {
        var result = await coverService.UpdateCoverAsync(id, coverDto);

        if (result > 0)
        {
            var auditLog = new AuditLog
            {
                EntityName = EntityNameEnum.Cover,
                EntityId = id,
                Type = HttpRequestTypeEnum.PUT,
            };

            await channelQueue.EnqueueAsync(auditLog);
            return ResponseModel<Cover>.SuccessResult(null, statusCode: HttpStatusCode.Accepted);
        }

        return ResponseModel<Cover>.FailureResult("Cover not found or failed to update.");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseModel<Cover>>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await coverService.DeleteCoverAsync(id);

        if (result != null)
        {
            var auditLog = new AuditLog
            {
                EntityName = EntityNameEnum.Cover,
                EntityId = result.Id,
                Type = HttpRequestTypeEnum.DELETE,
            };

            await channelQueue.EnqueueAsync(auditLog, cancellationToken);
            return ResponseModel<Cover>.SuccessResult(result);
        }

        logger.LogError("Cover not found or delete operation failed.");
        return ResponseModel<Cover>.FailureResult("Failed to delete cover");
    }
}
