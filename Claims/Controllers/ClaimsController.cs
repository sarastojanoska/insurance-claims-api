using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Application.Services.ClaimService;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimsController(IClaimService claimService, IChannelQueue<AuditLog> channelQueue, ILogger<ClaimsController> logger)
        : ControllerBase
    {
        [HttpGet]   // TODO: Complete swagger documentation
        public async Task<ActionResult<ResponseModel<IEnumerable<Claim>>>> GetClaimsAsync()
        {
            var claims = await claimService.GetClaimsAsync();
            return ResponseModel<IEnumerable<Claim>>.SuccessResult(claims);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<Claim>>> GetClaimAsync(string id)
        {
            var claim = await claimService.GetClaimAsync(id);

            if (claim != null)
                return ResponseModel<Claim>.SuccessResult(claim);

            return ResponseModel<Claim>.FailureResult("Claim not found");
        }

        [HttpPost]
        public async Task<ActionResult<ResponseModel<Claim>>> CreateAsync(CreateClaimDto claimDto)
        {
            var result = await claimService.CreateClaimAsync(claimDto);

            if (result != null)
            {
                var auditLog = new AuditLog
                {
                    EntityName = EntityNameEnum.Claim,
                    EntityId = result.Id,
                    Type = HttpRequestTypeEnum.POST,
                };

                await channelQueue.EnqueueAsync(auditLog);
                return ResponseModel<Claim>.SuccessResult(result, statusCode: HttpStatusCode.Created);
            }

            return ResponseModel<Claim>.FailureResult("Failed to create claim");
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseModel<Claim>>> UpdateAsync(string id, [FromBody] UpdateClaimDto claimDto)
        {
            var result = await claimService.UpdateClaimAsync(id, claimDto);

            if (result > 0)
            {
                var auditLog = new AuditLog
                {
                    EntityName = EntityNameEnum.Claim,
                    EntityId = id,
                    Type = HttpRequestTypeEnum.PUT
                };

                await channelQueue.EnqueueAsync(auditLog);
                return ResponseModel<Claim>.SuccessResult(null, statusCode: HttpStatusCode.Accepted);
            }

            return ResponseModel<Claim>.FailureResult("Claim not found or failed to update");
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResponseModel<Claim>>> DeleteAsync(string id)
        {
            var result = await claimService.DeleteClaimAsync(id);

            if (result != null)
            {
                var auditLog = new AuditLog
                {
                    EntityName = EntityNameEnum.Claim,
                    EntityId = result.Id,
                    Type = HttpRequestTypeEnum.DELETE
                };

                await channelQueue.EnqueueAsync(auditLog);
                return ResponseModel<Claim>.SuccessResult(result);
            }

            logger.LogError("Claim not found or delete operation failed.");
            return ResponseModel<Claim>.FailureResult("Failed to delete the claim");
        }
    }
}
