using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Claims.Tests
{
    public class CoversControllerTests
    {
        [Fact]
        public async Task Get_covers_returns_an_empty_collection_without_auditing()
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var response = await application.SendAsync<Cover[]>(client, HttpMethod.Get, "/api/covers", HttpStatusCode.OK);
            Assert.Empty(response.Data!);
            Assert.Empty(application.AuditQueue.Items);
        }

        [Fact]
        public async Task Cover_crud_persists_changes_recalculates_premium_and_audits_successful_writes()
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var cover = await application.CreateCoverAsync(client);
            Assert.False(string.IsNullOrWhiteSpace(cover.Id));
            Assert.Equal(application.CreatedAt.Date, cover.StartDate);
            Assert.Equal(application.CreatedAt.Date.AddDays(30), cover.EndDate);
            Assert.Equal(CoverType.Yacht, cover.Type);
            Assert.Equal(41250m, cover.Premium);
            var originalCreatedAt = cover.DateCreated;
            var route = $"/api/covers/{cover.Id}";
            var listed = await application.SendAsync<Cover[]>(client, HttpMethod.Get, "/api/covers", HttpStatusCode.OK);
            Assert.Equal(cover.Id, Assert.Single(listed.Data!).Id);
            var fetched = await application.SendAsync<Cover>(client, HttpMethod.Get, route, HttpStatusCode.OK);
            Assert.Equal(cover.Premium, fetched.Data!.Premium);

            var newEndDate = cover.StartDate.AddDays(60);
            var updated = await application.SendAsync<Cover>(client, HttpMethod.Put, route, HttpStatusCode.Accepted,
                new UpdateCoverDto(cover.StartDate, newEndDate, CoverType.Tanker));
            Assert.Null(updated.Data);
            fetched = await application.SendAsync<Cover>(client, HttpMethod.Get, route, HttpStatusCode.OK);
            Assert.Equal(CoverType.Tanker, fetched.Data!.Type);
            Assert.Equal(newEndDate, fetched.Data.EndDate);
            Assert.Equal(111375m, fetched.Data.Premium);
            Assert.Equal(originalCreatedAt, fetched.Data.DateCreated);

            var deleted = await application.SendAsync<Cover>(client, HttpMethod.Delete, route, HttpStatusCode.OK);
            Assert.Equal(cover.Id, deleted.Data!.Id);
            Assert.Equal(111375m, deleted.Data.Premium);
            await application.SendAsync<Cover>(client, HttpMethod.Get, route, HttpStatusCode.NotFound);
            listed = await application.SendAsync<Cover[]>(client, HttpMethod.Get, "/api/covers", HttpStatusCode.OK);
            Assert.Empty(listed.Data!);
            application.AssertAudits(EntityNameEnum.Cover, cover.Id,
                HttpRequestTypeEnum.POST, HttpRequestTypeEnum.PUT, HttpRequestTypeEnum.DELETE);
        }

        [Theory]
        [InlineData(false, "past", "StartDate cannot be in the past.")]
        [InlineData(false, "reversed", "EndDate cannot be earlier than StartDate.")]
        [InlineData(false, "too-long", "The insurance period cannot exceed one year.")]
        [InlineData(true, "past", "StartDate cannot be in the past.")]
        [InlineData(true, "reversed", "EndDate cannot be earlier than StartDate.")]
        [InlineData(true, "too-long", "The insurance period cannot exceed one year.")]
        public async Task Invalid_cover_dates_return_validation_errors_without_saving_or_auditing(
            bool update, string scenario, string error)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var existing = update ? await application.CreateCoverAsync(client) : null;
            var startDate = application.CreatedAt.Date;
            var endDate = startDate.AddDays(30);
            if (scenario == "past") startDate = startDate.AddDays(-1);
            if (scenario == "reversed") endDate = startDate.AddTicks(-1);
            if (scenario == "too-long") endDate = startDate.AddYears(1).AddTicks(1);

            var response = update
                ? await application.SendAsync<Cover>(client, HttpMethod.Put, $"/api/covers/{existing!.Id}", HttpStatusCode.BadRequest,
                    new UpdateCoverDto(startDate, endDate, CoverType.Tanker))
                : await application.SendAsync<Cover>(client, HttpMethod.Post, "/api/covers", HttpStatusCode.BadRequest,
                    new CreateCoverDto(startDate, endDate, CoverType.Tanker));
            Assert.Equal(error, response.Message);
            Assert.Null(response.Data);
            var listed = await application.SendAsync<Cover[]>(client, HttpMethod.Get, "/api/covers", HttpStatusCode.OK);
            if (existing is not null)
            {
                var saved = Assert.Single(listed.Data!);
                Assert.Equal(existing.Id, saved.Id);
                Assert.Equal(existing.StartDate, saved.StartDate);
                Assert.Equal(existing.EndDate, saved.EndDate);
                Assert.Equal(existing.Type, saved.Type);
                Assert.Equal(existing.Premium, saved.Premium);
                application.AssertAudits(EntityNameEnum.Cover, existing.Id, HttpRequestTypeEnum.POST);
            }
            else
            {
                Assert.Empty(listed.Data!);
                Assert.Empty(application.AuditQueue.Items);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Cover_creation_accepts_zero_days_and_exactly_one_calendar_year(bool fullYear)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var startDate = application.CreatedAt.Date;
            var endDate = fullYear ? startDate.AddYears(1) : startDate;
            var response = await application.SendAsync<Cover>(client, HttpMethod.Post, "/api/covers", HttpStatusCode.Created,
                new CreateCoverDto(startDate, endDate, CoverType.Yacht));
            Assert.Equal(startDate, response.Data!.StartDate);
            Assert.Equal(endDate, response.Data.EndDate);
            if (fullYear) Assert.True(response.Data.Premium > 0m);
            else Assert.Equal(0m, response.Data.Premium);
            application.AssertAudits(EntityNameEnum.Cover, response.Data.Id, HttpRequestTypeEnum.POST);
        }

        [Theory]
        [InlineData("GET", HttpStatusCode.NotFound, "Not found")]
        [InlineData("PUT", HttpStatusCode.BadRequest, "Cover not found.")]
        [InlineData("DELETE", HttpStatusCode.BadRequest, "Cover not found.")]
        public async Task Missing_cover_returns_the_documented_status_without_auditing(string method, HttpStatusCode status, string error)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var response = await application.SendAsync<Cover>(client, new HttpMethod(method), "/api/covers/missing", status,
                method == "PUT" ? new UpdateCoverDto(application.CreatedAt.Date, application.CreatedAt.Date.AddDays(30), CoverType.Yacht) : null);
            Assert.Equal(error, response.Message);
            Assert.Null(response.Data);
            Assert.Empty(application.AuditQueue.Items);
        }

        [Theory]
        [InlineData(CoverType.Yacht, "42556.25")]
        [InlineData(CoverType.PassengerShip, "46470")]
        [InlineData(CoverType.ContainerShip, "50342.50")]
        [InlineData(CoverType.BulkCarrier, "50342.50")]
        [InlineData(CoverType.Tanker, "58087.50")]
        public async Task Compute_premium_uses_real_rates_without_saving_or_auditing(CoverType type, string expectedPremium)
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            var startDate = application.CreatedAt.Date;
            var route = $"/api/covers/compute?startDate={startDate:yyyy-MM-dd}&endDate={startDate.AddDays(31):yyyy-MM-dd}&coverType={type}";
            var response = await application.SendAsync<decimal>(client, HttpMethod.Post, route, HttpStatusCode.OK);
            Assert.Equal(decimal.Parse(expectedPremium, System.Globalization.CultureInfo.InvariantCulture), response.Data);
            var listed = await application.SendAsync<Cover[]>(client, HttpMethod.Get, "/api/covers", HttpStatusCode.OK);
            Assert.Empty(listed.Data!);
            Assert.Empty(application.AuditQueue.Items);
        }

        [Fact]
        public async Task Invalid_enum_name_returns_validation_problem_details_without_saving_or_auditing()
        {
            using var application = new ApiWebApplicationFactory();
            using var client = application.CreateApiClient();
            using var body = JsonContent.Create(new
            {
                startDate = application.CreatedAt.Date,
                endDate = application.CreatedAt.Date.AddDays(30),
                type = "NotACoverType"
            });
            using var response = await client.PostAsync("/api/covers", body, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.True(problem.GetProperty("errors").EnumerateObject().Any());
            var listed = await application.SendAsync<Cover[]>(client, HttpMethod.Get, "/api/covers", HttpStatusCode.OK);
            Assert.Empty(listed.Data!);
            Assert.Empty(application.AuditQueue.Items);
        }
    }
}