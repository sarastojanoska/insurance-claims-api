using Application.DTOs;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Claims.Tests
{
    public class ClaimsControllerTests
    {
        [Fact]
        public async Task Get_Claims()
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            using var response = await client.GetAsync("/api/claims", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ResponseModel<Claim[]>>(TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
            Assert.Empty(application.AuditQueue.Items);
        }

        [Fact]
        public async Task Claim_crud_persists_changes_and_audits_successful_writes()
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var cover = await application.CreateCoverAsync(client);
            var created = await application.SendAsync<Claim>(client, HttpMethod.Post, "/api/claims", HttpStatusCode.Created,
                new CreateClaimDto(cover.Id, "Engine fire", ClaimType.Fire, 100_000m));
            Assert.NotNull(created.Data);
            var claim = created.Data;
            Assert.False(string.IsNullOrWhiteSpace(claim.Id));
            Assert.Equal(cover.Id, claim.CoverId);
            Assert.Equal("Engine fire", claim.Name);
            Assert.Equal(ClaimType.Fire, claim.Type);
            Assert.Equal(100_000m, claim.DamageCost);
            Assert.Equal(application.CreatedAt, claim.DateCreated);
            var route = $"/api/claims/{claim.Id}";

            var listed = await application.SendAsync<Claim[]>(client, HttpMethod.Get, "/api/claims", HttpStatusCode.OK);
            Assert.Equal(claim.Id, Assert.Single(listed.Data!).Id);
            var fetched = await application.SendAsync<Claim>(client, HttpMethod.Get, route, HttpStatusCode.OK);
            Assert.Equal(claim.Name, fetched.Data!.Name);
            var updated = await application.SendAsync<Claim>(client, HttpMethod.Put, route, HttpStatusCode.Accepted,
                new UpdateClaimDto("Collision damage", ClaimType.Collision, 500m));
            Assert.Null(updated.Data);
            fetched = await application.SendAsync<Claim>(client, HttpMethod.Get, route, HttpStatusCode.OK);
            Assert.Equal("Collision damage", fetched.Data!.Name);
            Assert.Equal(ClaimType.Collision, fetched.Data.Type);
            Assert.Equal(500m, fetched.Data.DamageCost);
            Assert.Equal(application.CreatedAt, fetched.Data.DateCreated);
            Assert.Equal(cover.Id, fetched.Data.CoverId);

            var deleted = await application.SendAsync<Claim>(client, HttpMethod.Delete, route, HttpStatusCode.OK);
            Assert.Equal(claim.Id, deleted.Data!.Id);
            Assert.Equal("Collision damage", deleted.Data.Name);
            await application.SendAsync<Claim>(client, HttpMethod.Get, route, HttpStatusCode.BadRequest);
            listed = await application.SendAsync<Claim[]>(client, HttpMethod.Get, "/api/claims", HttpStatusCode.OK);
            Assert.Empty(listed.Data!);
            application.AssertAudits(EntityNameEnum.Claim, claim.Id,
                HttpRequestTypeEnum.POST, HttpRequestTypeEnum.PUT, HttpRequestTypeEnum.DELETE);
            application.AssertAudits(EntityNameEnum.Cover, cover.Id, HttpRequestTypeEnum.POST);
        }

        [Theory]
        [InlineData("missing-cover", "Cover not found.")]
        [InlineData("excessive-cost", "DamageCost cannot exceed 100,000.")]
        [InlineData("before-cover", "Claim creation date must be within the related cover period.")]
        [InlineData("after-cover", "Claim creation date must be within the related cover period.")]
        public async Task Invalid_claim_creation_returns_validation_error_without_persisting_or_auditing(string scenario, string error)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var cover = new Cover
            {
                StartDate = application.CreatedAt.AddDays(-1),
                EndDate = application.CreatedAt.AddDays(1)
            };
            if (scenario == "before-cover") cover.StartDate = application.CreatedAt.AddTicks(1);
            if (scenario == "after-cover") cover.EndDate = application.CreatedAt.AddTicks(-1);
            if (scenario != "missing-cover") await application.SeedAsync(cover);

            var response = await application.SendAsync<Claim>(client, HttpMethod.Post, "/api/claims", HttpStatusCode.BadRequest,
                new CreateClaimDto(cover.Id, "Invalid claim", ClaimType.Fire, scenario == "excessive-cost" ? 100_000.01m : 100m));
            Assert.Equal(error, response.Message);
            Assert.Null(response.Data);
            var listed = await application.SendAsync<Claim[]>(client, HttpMethod.Get, "/api/claims", HttpStatusCode.OK);
            Assert.Empty(listed.Data!);
            Assert.Empty(application.AuditQueue.Items);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Claim_creation_accepts_both_cover_timestamp_boundaries(bool startBoundary)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var cover = new Cover
            {
                StartDate = startBoundary ? application.CreatedAt : application.CreatedAt.AddDays(-1),
                EndDate = startBoundary ? application.CreatedAt.AddDays(1) : application.CreatedAt
            };
            await application.SeedAsync(cover);
            var response = await application.SendAsync<Claim>(client, HttpMethod.Post, "/api/claims", HttpStatusCode.Created,
                new CreateClaimDto(cover.Id, "Boundary claim", ClaimType.Fire, 100m));
            Assert.Equal(application.CreatedAt, response.Data!.DateCreated);
            application.AssertAudits(EntityNameEnum.Claim, response.Data.Id, HttpRequestTypeEnum.POST);
        }

        [Theory]
        [InlineData("excessive-cost", "DamageCost cannot exceed 100,000.")]
        [InlineData("missing-cover", "Related cover not found.")]
        [InlineData("outside-cover", "Claim creation date must be within the related cover period.")]
        public async Task Invalid_claim_update_preserves_saved_values_and_does_not_audit(string scenario, string error)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var cover = new Cover { StartDate = application.CreatedAt, EndDate = application.CreatedAt.AddDays(1) };
            if (scenario != "missing-cover") await application.SeedAsync(cover);
            var claim = new Claim
            {
                CoverId = cover.Id, Name = "Original", Type = ClaimType.Fire, DamageCost = 50m,
                DateCreated = scenario == "outside-cover" ? application.CreatedAt.AddTicks(-1) : application.CreatedAt
            };
            await application.SeedAsync(claim);
            var route = $"/api/claims/{claim.Id}";
            var response = await application.SendAsync<Claim>(client, HttpMethod.Put, route, HttpStatusCode.BadRequest,
                new UpdateClaimDto("Changed", ClaimType.Collision, scenario == "excessive-cost" ? 100_000.01m : 100m));
            Assert.Equal(error, response.Message);
            var fetched = await application.SendAsync<Claim>(client, HttpMethod.Get, route, HttpStatusCode.OK);
            Assert.Equal("Original", fetched.Data!.Name);
            Assert.Equal(50m, fetched.Data.DamageCost);
            Assert.Equal(ClaimType.Fire, fetched.Data.Type);
            Assert.Equal(claim.DateCreated, fetched.Data.DateCreated);
            Assert.Empty(application.AuditQueue.Items);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task Missing_claim_returns_bad_request_without_auditing(string method)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var response = await application.SendAsync<Claim>(client, new HttpMethod(method), "/api/claims/missing", HttpStatusCode.BadRequest,
                method == "PUT" ? new UpdateClaimDto("Missing", ClaimType.Fire, 100m) : null);
            var expectedMessage = method switch
            {
                "GET" => "Claim not found",
                "PUT" => "Claim not found.",
                _ => "Claim with ID 'missing' was not found."
            };
            Assert.Equal(expectedMessage, response.Message);
            Assert.Null(response.Data);
            Assert.Empty(application.AuditQueue.Items);
        }

    }
}
