using Application.Claims.Commands;
using Application.Claims.Queries;
using Application.DTOs;
using Application.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimsController(ISender sender)
        : ControllerBase
    {
        [HttpGet]  
        [SwaggerOperation(Summary = "List all claims",
            Description = "Returns HTTP 200 with all claims in ResponseModel.data. An empty collection returns an empty array. No pagination, filtering, or sorting parameters are supported.")]
        public async Task<ActionResult<ResponseModel<IEnumerable<Claim>>>> GetClaimsAsync(CancellationToken cancellationToken = default)
            => await sender.Send(new GetClaimsQuery(), cancellationToken);

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a claim by ID",
            Description = "Accepts a claim ID in the route and returns HTTP 200 with the claim in ResponseModel.data. A missing claim returns HTTP 400 with the message 'Claim not found'.")]
        public async Task<ActionResult<ResponseModel<Claim>>> GetClaimAsync(string id, CancellationToken cancellationToken = default)
            => await sender.Send(new GetClaimQuery(id), cancellationToken);

        [HttpPost]
        [SwaggerOperation(Summary = "Create a claim",
            Description = "Accepts coverId, name, type, and damageCost in a JSON body. Claim types are Collision, Grounding, BadWeather, and Fire. The related cover must exist, damageCost cannot exceed 100,000, and the server-generated UTC creation timestamp must be within the cover period, including both boundaries. Returns HTTP 201 with the created claim, or HTTP 400 with the service validation message. Successful creation enqueues an audit record.")]
        public async Task<ActionResult<ResponseModel<Claim>>> CreateAsync(CreateClaimDto claimDto, CancellationToken cancellationToken = default)
            => await sender.Send(new CreateClaimCommand(claimDto), cancellationToken);

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update a claim",
            Description = "Accepts a claim ID in the route and name, type, and damageCost in a JSON body. The cover association and creation timestamp cannot be changed. Damage cost cannot exceed 100,000, and the original creation timestamp must remain within the related cover period. Returns HTTP 202 with null data after saving, or HTTP 400 when the claim or related cover is missing or validation fails. Successful update enqueues an audit record.")]
        public async Task<ActionResult<ResponseModel<Claim>>> UpdateAsync(string id, [FromBody] UpdateClaimDto claimDto, CancellationToken cancellationToken = default)
            => await sender.Send(new UpdateClaimCommand(id, claimDto), cancellationToken);

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a claim",
            Description = "Accepts a claim ID in the route and no request body. Returns HTTP 200 with the deleted claim in ResponseModel.data, or HTTP 400 when the claim is missing. Successful deletion enqueues an audit record.")]
        public async Task<ActionResult<ResponseModel<Claim>>> DeleteAsync(string id, CancellationToken cancellationToken = default)
            => await sender.Send(new DeleteClaimCommand(id), cancellationToken);
    }
}
