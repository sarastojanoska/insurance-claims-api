using Application.DTOs;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Claims.Tests.IntegrationTests
{
    public class ClaimsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ClaimsControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<Cover> CreateCoverAsync()
        {
            var dto = new CreateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(6), CoverType.Yacht);
            var response = await _client.PostAsJsonAsync("/api/covers", dto);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);
            return body!.Data!;
        }

        [Fact]
        public async Task CreateClaim_ReturnsCreated_WhenValid()
        {
            var cover = await CreateCoverAsync();
            var createdDate = cover.StartDate.AddDays(1);
            var dto = new CreateClaimDto(cover.Id, "Hull damage", ClaimType.Collision, 500m, createdDate);

            var response = await _client.PostAsJsonAsync("/api/claims", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);
            Assert.True(body!.Success);
            Assert.Equal("Hull damage", body.Data!.Name);
        }

        [Fact]
        public async Task CreateClaim_ReturnsBadRequest_WhenCoverNotFound()
        {
            var dto = new CreateClaimDto("missing-cover-id", "Damage", ClaimType.Collision, 500m, DateTime.Today);

            var response = await _client.PostAsJsonAsync("/api/claims", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);
            Assert.False(body!.Success);
            Assert.Equal("Cover not found.", body.Message);
        }

        [Fact]
        public async Task CreateClaim_ReturnsBadRequest_WhenDamageCostExceedsLimit()
        {
            var cover = await CreateCoverAsync();
            var dto = new CreateClaimDto(cover.Id, "Damage", ClaimType.Collision, 200_000m, cover.StartDate.AddDays(1));

            var response = await _client.PostAsJsonAsync("/api/claims", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetClaim_ReturnsBadRequest_WhenClaimNotFound()
        {
            var response = await _client.GetAsync("/api/claims/missing-id");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);
            Assert.False(body!.Success);
            Assert.Equal("Claim not found", body.Message);
        }

        [Fact]
        public async Task GetClaim_ReturnsClaim_WhenFound()
        {
            var cover = await CreateCoverAsync();
            var createDto = new CreateClaimDto(cover.Id, "Fire damage", ClaimType.Fire, 300m, cover.StartDate.AddDays(2));
            var createResponse = await _client.PostAsJsonAsync("/api/claims", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);

            var response = await _client.GetAsync($"/api/claims/{created!.Data!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);
            Assert.Equal("Fire damage", body!.Data!.Name);
        }

        [Fact]
        public async Task GetClaims_ReturnsOkWithList()
        {
            var response = await _client.GetAsync("/api/claims");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ResponseModel<List<Claim>>>(JsonOptions);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
        }

        [Fact]
        public async Task UpdateClaim_ReturnsOk_WhenValid()
        {
            var cover = await CreateCoverAsync();
            var createDto = new CreateClaimDto(cover.Id, "Original", ClaimType.Collision, 100m, cover.StartDate.AddDays(1));
            var createResponse = await _client.PostAsJsonAsync("/api/claims", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);

            var updateDto = new UpdateClaimDto("Updated name", ClaimType.BadWeather, 400m);
            var response = await _client.PutAsJsonAsync($"/api/claims/{created!.Data!.Id}", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var getResponse = await _client.GetAsync($"/api/claims/{created.Data.Id}");
            var getBody = await getResponse.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);
            Assert.Equal("Updated name", getBody!.Data!.Name);
        }

        [Fact]
        public async Task UpdateClaim_ReturnsBadRequest_WhenClaimNotFound()
        {
            var dto = new UpdateClaimDto("Name", ClaimType.Collision, 100m);

            var response = await _client.PutAsJsonAsync("/api/claims/missing-id", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteClaim_ReturnsNoContent_WhenValid()
        {
            var cover = await CreateCoverAsync();
            var createDto = new CreateClaimDto(cover.Id, "ToDelete", ClaimType.Collision, 100m, cover.StartDate.AddDays(1));
            var createResponse = await _client.PostAsJsonAsync("/api/claims", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<ResponseModel<Claim>>(JsonOptions);

            var response = await _client.DeleteAsync($"/api/claims/{created!.Data!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await _client.GetAsync($"/api/claims/{created.Data.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteClaim_ReturnsBadRequest_WhenClaimNotFound()
        {
            var response = await _client.DeleteAsync("/api/claims/missing-id");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
