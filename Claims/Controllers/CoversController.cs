using Application.Covers.Commands;
using Application.Covers.Queries;
using Application.DTOs;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoversController(ISender sender) : ControllerBase
    {
        [HttpPost("compute")]
        [SwaggerOperation(Summary = "Compute a cover premium",
            Description = "Accepts startDate, endDate, and coverType as query parameters; no JSON body is required. Returns HTTP 200 with a numeric premium in ResponseModel.data without saving a cover or creating an audit. The base daily rate is 1250, increased by 10% for Yacht, 20% for PassengerShip, 50% for Tanker, and 30% for other defined types. The first 30 days use the full rate; the next 150 days receive a 5% Yacht or 2% other-type discount; remaining days receive an 8% Yacht or 3% other-type total discount. Calendar days are counted with the end date excluded. Unlike cover creation, past dates and periods longer than one year are allowed. Reversed calendar-date ranges and undefined cover types return HTTP 400.")]
        public async Task<ActionResult<ResponseModel<decimal>>> ComputePremiumAsync(DateTime startDate, DateTime endDate, CoverType coverType, CancellationToken cancellationToken = default)
            => await sender.Send(new ComputePremiumQuery(startDate, endDate, coverType), cancellationToken);

        [HttpGet]
        [SwaggerOperation(Summary = "List all covers",
            Description = "Returns HTTP 200 with all covers in ResponseModel.data. An empty collection returns an empty array. No pagination, filtering, or sorting parameters are supported.")]
        public async Task<ActionResult<ResponseModel<IEnumerable<Cover>>>> GetAsync(CancellationToken cancellationToken = default)
            => await sender.Send(new GetCoversQuery(), cancellationToken);

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a cover by ID",
            Description = "Accepts a cover ID in the route and returns HTTP 200 with the cover and calculated premium in ResponseModel.data. A missing cover returns HTTP 404 with the message 'Not found'.")]
        public async Task<ActionResult<ResponseModel<Cover>>> GetAsync(string id, CancellationToken cancellationToken = default)
            => await sender.Send(new GetCoverQuery(id), cancellationToken);

        [HttpPost]
        [SwaggerOperation(Summary = "Create a cover",
            Description = "Accepts startDate, endDate, and type in a JSON body. Cover types are Yacht, PassengerShip, ContainerShip, BulkCarrier, and Tanker; undefined values are rejected. The start calendar date cannot precede the server's local current date. End date cannot precede start date or exceed startDate.AddYears(1). The premium is calculated automatically. Returns HTTP 201 with the created cover, or HTTP 400 with the service validation message. Successful creation enqueues an audit record.")]
        public async Task<ActionResult<ResponseModel<Cover>>> CreateAsync(CreateCoverDto coverDto, CancellationToken cancellationToken)
            => await sender.Send(new CreateCoverCommand(coverDto), cancellationToken);

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update a cover",
            Description = "Accepts a cover ID in the route and startDate, endDate, and type in a JSON body. The same date and type rules as cover creation apply, including rejecting start dates in the past. The premium is recalculated automatically. Returns HTTP 200 with null data after saving, HTTP 404 when the cover is missing, or HTTP 400 when validation fails. Retrieve the cover to see its updated premium. By design, existing claims retain their original timestamps and are not revalidated on cover updates, so they can fall outside the changed period; later claim updates validate against the current cover dates. Successful update enqueues an audit record.")]
        public async Task<ActionResult<ResponseModel<Cover>>> UpdateAsync(string id, [FromBody] UpdateCoverDto coverDto, CancellationToken cancellationToken = default)
            => await sender.Send(new UpdateCoverCommand(id, coverDto), cancellationToken);

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a cover",
            Description = "Accepts a cover ID in the route and no request body. Returns HTTP 204 with no response body after deletion, or HTTP 404 when the cover is missing. Related claims are not explicitly deleted and their existence does not block this operation. Successful deletion enqueues an audit record.")]
        public async Task<ActionResult<ResponseModel<Cover>>> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new DeleteCoverCommand(id), cancellationToken);
            return response.Success ? NoContent() : response;
        }
    }
}
