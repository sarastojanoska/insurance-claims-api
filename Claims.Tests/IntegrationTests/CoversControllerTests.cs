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
    public class CoversControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public CoversControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ComputePremium_ReturnsOk_WhenValid()
        {
            var startDate = DateTime.Today.AddDays(1).ToString("o");
            var endDate = DateTime.Today.AddMonths(1).ToString("o");

            var response = await _client.PostAsync(
                $"/api/covers/compute?startDate={Uri.EscapeDataString(startDate)}&endDate={Uri.EscapeDataString(endDate)}&coverType=Yacht",
                content: null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ResponseModel<decimal>>(JsonOptions);
            Assert.True(body!.Success);
            Assert.True(body.Data > 0);
        }

        [Fact]
        public async Task ComputePremium_ReturnsBadRequest_WhenEndDateBeforeStartDate()
        {
            var startDate = DateTime.Today.ToString("o");
            var endDate = DateTime.Today.AddDays(-1).ToString("o");

            var response = await _client.PostAsync(
                $"/api/covers/compute?startDate={Uri.EscapeDataString(startDate)}&endDate={Uri.EscapeDataString(endDate)}&coverType=Yacht",
                content: null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateCover_ReturnsCreated_WhenValid()
        {
            var dto = new CreateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(3), CoverType.Tanker);

            var response = await _client.PostAsJsonAsync("/api/covers", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);
            Assert.True(body!.Success);
            Assert.Equal(CoverType.Tanker, body.Data!.Type);
            Assert.True(body.Data.Premium > 0);
        }

        [Fact]
        public async Task CreateCover_ReturnsBadRequest_WhenStartDateInPast()
        {
            var dto = new CreateCoverDto(DateTime.Today.AddDays(-5), DateTime.Today.AddMonths(1), CoverType.Yacht);

            var response = await _client.PostAsJsonAsync("/api/covers", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCover_ReturnsNotFound_WhenMissing()
        {
            var response = await _client.GetAsync("/api/covers/missing-id");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCover_ReturnsCover_WhenFound()
        {
            var createDto = new CreateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(2), CoverType.PassengerShip);
            var createResponse = await _client.PostAsJsonAsync("/api/covers", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);

            var response = await _client.GetAsync($"/api/covers/{created!.Data!.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);
            Assert.Equal(CoverType.PassengerShip, body!.Data!.Type);
        }

        [Fact]
        public async Task GetCovers_ReturnsOkWithList()
        {
            var response = await _client.GetAsync("/api/covers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<ResponseModel<List<Cover>>>(JsonOptions);
            Assert.True(body!.Success);
            Assert.NotNull(body.Data);
        }

        [Fact]
        public async Task UpdateCover_ReturnsOk_WhenValid()
        {
            var createDto = new CreateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(1), CoverType.Yacht);
            var createResponse = await _client.PostAsJsonAsync("/api/covers", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);

            var updateDto = new UpdateCoverDto(DateTime.Today.AddDays(2), DateTime.Today.AddMonths(2), CoverType.BulkCarrier);
            var response = await _client.PutAsJsonAsync($"/api/covers/{created!.Data!.Id}", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var getResponse = await _client.GetAsync($"/api/covers/{created.Data.Id}");
            var getBody = await getResponse.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);
            Assert.Equal(CoverType.BulkCarrier, getBody!.Data!.Type);
        }

        [Fact]
        public async Task UpdateCover_ReturnsNotFound_WhenCoverMissing()
        {
            var dto = new UpdateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(1), CoverType.Yacht);

            var response = await _client.PutAsJsonAsync("/api/covers/missing-id", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCover_ReturnsNoContent_WhenValid()
        {
            var createDto = new CreateCoverDto(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(1), CoverType.Yacht);
            var createResponse = await _client.PostAsJsonAsync("/api/covers", createDto);
            var created = await createResponse.Content.ReadFromJsonAsync<ResponseModel<Cover>>(JsonOptions);

            var response = await _client.DeleteAsync($"/api/covers/{created!.Data!.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await _client.GetAsync($"/api/covers/{created.Data.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteCover_ReturnsNotFound_WhenCoverMissing()
        {
            var response = await _client.DeleteAsync("/api/covers/missing-id");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
