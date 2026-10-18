using API.Controllers;
using API.Extensions;
using API.Filters;
using Application.Abstractions;
using Application.Claims.Commands;
using Application.Claims.Queries;
using Application.Covers.Commands;
using Application.Covers.Queries;
using Application.DTOs;
using Application.Models;
using Application.Services.ClaimService;
using Application.Services.CoverService;
using Application.Services.PremiumCalculationService;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Swashbuckle.AspNetCore.Annotations;
using Xunit;

namespace Claims.Tests
{
    public class CqrsTests
    {
        [Fact]
        public async Task Swagger_document_contains_annotations_for_every_endpoint()
        {
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddControllers().AddApplicationPart(typeof(ClaimsController).Assembly);
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen(options => options.EnableAnnotations());
                })
                .Configure(application => application.UseSwagger()));
            using var client = server.CreateClient();
            using var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var document = JsonDocument.Parse(json);
            var annotations = new[] { typeof(ClaimsController), typeof(CoversController) }
                .SelectMany(controller => controller.GetMethods())
                .Select(method => method.GetCustomAttribute<SwaggerOperationAttribute>())
                .Where(annotation => annotation is not null)
                .ToDictionary(annotation => annotation!.Summary, annotation => annotation!.Description);
            var operations = document.RootElement.GetProperty("paths").EnumerateObject()
                .SelectMany(path => path.Value.EnumerateObject()).ToArray();

            Assert.Equal(11, annotations.Count);
            Assert.Equal(annotations.Count, operations.Length);
            foreach (var operation in operations)
            {
                var summary = operation.Value.GetProperty("summary").GetString();
                var description = operation.Value.GetProperty("description").GetString();
                Assert.False(string.IsNullOrWhiteSpace(summary));
                Assert.False(string.IsNullOrWhiteSpace(description));
                Assert.True(annotations.TryGetValue(summary!, out var expectedDescription));
                Assert.Equal(expectedDescription, description);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Http_endpoints_apply_response_status_codes_and_preserve_errors(bool success)
        {
            using var fixture = new Fixture(success);
            using var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddControllers(options => options.Filters.Add<ResponseModelStatusCodeFilter>())
                        .AddApplicationPart(typeof(ClaimsController).Assembly);
                    fixture.RegisterServices(services);
                    services.RemoveAll<IHostedService>();
                })
                .Configure(application =>
                {
                    application.UseRouting();
                    application.UseEndpoints(endpoints => endpoints.MapControllers());
                }));
            using var client = server.CreateClient();
            var cancellationToken = TestContext.Current.CancellationToken;

            await AssertHttpResponseAsync<IEnumerable<Claim>>(await client.GetAsync("/api/claims", cancellationToken), HttpStatusCode.OK, true);
            await AssertHttpResponseAsync<Claim>(await client.GetAsync($"/api/claims/{fixture.Claims.Item.Id}", cancellationToken),
                success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, success);
            await AssertHttpResponseAsync<Claim>(await client.PostAsJsonAsync("/api/claims", fixture.Claims.CreateDto, cancellationToken),
                success ? HttpStatusCode.Created : HttpStatusCode.BadRequest, success, fixture.Claims.CreateResult.Error);
            await AssertHttpResponseAsync<Claim>(await client.PutAsJsonAsync($"/api/claims/{fixture.Claims.Item.Id}", fixture.Claims.UpdateDto, cancellationToken),
                success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, success, fixture.Claims.WriteResult.Error);
            await AssertHttpResponseAsync<Claim>(await client.DeleteAsync($"/api/claims/{fixture.Claims.Item.Id}", cancellationToken),
                success ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest, success, fixture.Claims.WriteResult.Error);

            await AssertHttpResponseAsync<IEnumerable<Cover>>(await client.GetAsync("/api/covers", cancellationToken), HttpStatusCode.OK, true);
            await AssertHttpResponseAsync<Cover>(await client.GetAsync($"/api/covers/{fixture.Covers.Item.Id}", cancellationToken),
                success ? HttpStatusCode.OK : HttpStatusCode.NotFound, success);
            await AssertHttpResponseAsync<Cover>(await client.PostAsJsonAsync("/api/covers", fixture.Covers.CreateDto, cancellationToken),
                success ? HttpStatusCode.Created : HttpStatusCode.BadRequest, success, fixture.Covers.WriteResult.Error);
            await AssertHttpResponseAsync<Cover>(await client.PutAsJsonAsync($"/api/covers/{fixture.Covers.Item.Id}", fixture.Covers.UpdateDto, cancellationToken),
                success ? HttpStatusCode.OK : HttpStatusCode.BadRequest, success, fixture.Covers.WriteResult.Error);
            await AssertHttpResponseAsync<Cover>(await client.DeleteAsync($"/api/covers/{fixture.Covers.Item.Id}", cancellationToken),
                success ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest, success, fixture.Covers.WriteResult.Error);
            var premiumDto = fixture.Covers.CreateDto;
            using var premiumRequest = new HttpRequestMessage(HttpMethod.Post,
                $"/api/covers/compute?startDate={premiumDto.StartDate:yyyy-MM-dd}&endDate={premiumDto.EndDate:yyyy-MM-dd}&coverType=Yacht");
            var premium = await AssertHttpResponseAsync<decimal>(await client.SendAsync(premiumRequest, cancellationToken), HttpStatusCode.OK, true);
            Assert.Equal(123m, premium.Data);
            Assert.Equal(success ? 6 : 0, fixture.Queue.Items.Count);
        }

        private static async Task<ResponseModel<T>> AssertHttpResponseAsync<T>(
            HttpResponseMessage response, HttpStatusCode expectedStatus, bool success, string? error = null)
        {
            using (response)
            {
                Assert.Equal(expectedStatus, response.StatusCode);
                if (expectedStatus == HttpStatusCode.NoContent)
                {
                    Assert.Empty(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
                    return ResponseModel<T>.SuccessResult(default, statusCode: HttpStatusCode.NoContent);
                }
                var model = await response.Content.ReadFromJsonAsync<ResponseModel<T>>(TestContext.Current.CancellationToken);
                Assert.NotNull(model);
                Assert.Equal((int)expectedStatus, model.StatusCode);
                Assert.Equal(success, model.Success);
                if (error is not null)
                    Assert.Equal(error, model.Message);
                return model;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Claims_endpoints_dispatch_requests_and_audit_only_successful_writes(bool success)
        {
            using var fixture = new Fixture(success);
            var controller = new ClaimsController(fixture.Sender);
            var cancellationToken = TestContext.Current.CancellationToken;

            var claims = (await controller.GetClaimsAsync(cancellationToken)).Value!;
            Assert.Same(fixture.Claims.Items, claims.Data);
            var claim = (await controller.GetClaimAsync(fixture.Claims.Item.Id, cancellationToken)).Value!;
            Assert.Equal(success, claim.Success);
            Assert.Same(success ? fixture.Claims.Item : null, claim.Data);
            Assert.Empty(fixture.Queue.Items);

            var created = (await controller.CreateAsync(fixture.Claims.CreateDto, cancellationToken)).Value!;
            Assert.Equal(success, created.Success);
            Assert.Equal(success ? 201 : 400, created.StatusCode);
            Assert.Same(success ? fixture.Claims.Item : null, created.Data);
            var updated = (await controller.UpdateAsync(fixture.Claims.Item.Id, fixture.Claims.UpdateDto, cancellationToken)).Value!;
            Assert.Equal(success, updated.Success);
            Assert.Equal(success ? 200 : 400, updated.StatusCode);
            Assert.Null(updated.Data);
            var deleted = await controller.DeleteAsync(fixture.Claims.Item.Id, cancellationToken);
            if (success) Assert.IsType<NoContentResult>(deleted.Result);
            else Assert.False(deleted.Value!.Success);

            AssertAudits(fixture.Queue, success, EntityNameEnum.Claim, fixture.Claims.Item.Id);
            Assert.Equal(5, fixture.Claims.Calls);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Covers_endpoints_dispatch_requests_and_audit_only_successful_writes(bool success)
        {
            using var fixture = new Fixture(success);
            var controller = new CoversController(fixture.Sender);
            var cancellationToken = TestContext.Current.CancellationToken;

            var premium = (await controller.ComputePremiumAsync(
                fixture.Covers.CreateDto.StartDate, fixture.Covers.CreateDto.EndDate, fixture.Covers.CreateDto.Type, cancellationToken)).Value!;
            Assert.True(premium.Success);
            Assert.Equal(200, premium.StatusCode);
            Assert.Equal(123m, premium.Data);
            var covers = (await controller.GetAsync(cancellationToken)).Value!;
            Assert.Same(fixture.Covers.Items, covers.Data);
            var cover = (await controller.GetAsync(fixture.Covers.Item.Id, cancellationToken)).Value!;
            Assert.Equal(success, cover.Success);
            Assert.Equal(success ? 200 : 404, cover.StatusCode);
            Assert.Same(success ? fixture.Covers.Item : null, cover.Data);
            Assert.Empty(fixture.Queue.Items);

            var created = (await controller.CreateAsync(fixture.Covers.CreateDto, cancellationToken)).Value!;
            Assert.Equal(success, created.Success);
            Assert.Equal(success ? 201 : 400, created.StatusCode);
            Assert.Same(success ? fixture.Covers.Item : null, created.Data);
            var updated = (await controller.UpdateAsync(fixture.Covers.Item.Id, fixture.Covers.UpdateDto, cancellationToken)).Value!;
            Assert.Equal(success, updated.Success);
            Assert.Equal(success ? 200 : 400, updated.StatusCode);
            Assert.Null(updated.Data);
            var deleted = await controller.DeleteAsync(fixture.Covers.Item.Id, cancellationToken);
            if (success) Assert.IsType<NoContentResult>(deleted.Result);
            else Assert.False(deleted.Value!.Success);

            AssertAudits(fixture.Queue, success, EntityNameEnum.Cover, fixture.Covers.Item.Id);
            Assert.Equal(5, fixture.Covers.Calls);
            Assert.Equal(1, fixture.Premium.Calls);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task All_handlers_map_responses_and_audit_only_successful_writes(bool success)
        {
            using var fixture = new Fixture(success);
            var cancellationToken = TestContext.Current.CancellationToken;
            var claims = await fixture.Sender.Send(new GetClaimsQuery(), cancellationToken);
            Assert.True(claims.Success);
            Assert.Same(fixture.Claims.Items, claims.Data);
            var claim = await fixture.Sender.Send(new GetClaimQuery(fixture.Claims.Item.Id), cancellationToken);
            Assert.Equal(success, claim.Success);
            Assert.Same(success ? fixture.Claims.Item : null, claim.Data);
            Assert.Equal(success ? "Success" : "Claim not found", claim.Message);

            var covers = await fixture.Sender.Send(new GetCoversQuery(), cancellationToken);
            Assert.True(covers.Success);
            Assert.Same(fixture.Covers.Items, covers.Data);
            var cover = await fixture.Sender.Send(new GetCoverQuery(fixture.Covers.Item.Id), cancellationToken);
            Assert.Equal(success, cover.Success);
            Assert.Equal(success ? 200 : 404, cover.StatusCode);
            Assert.Same(success ? fixture.Covers.Item : null, cover.Data);
            var premium = await fixture.Sender.Send(new ComputePremiumQuery(
                fixture.Covers.CreateDto.StartDate, fixture.Covers.CreateDto.EndDate, fixture.Covers.CreateDto.Type), cancellationToken);
            Assert.True(premium.Success);
            Assert.Equal(123m, premium.Data);
            Assert.Empty(fixture.Queue.Items);

            var createdClaim = await fixture.Sender.Send(new CreateClaimCommand(fixture.Claims.CreateDto), cancellationToken);
            Assert.Equal(success, createdClaim.Success);
            Assert.Equal(success ? 201 : 400, createdClaim.StatusCode);
            Assert.Same(success ? fixture.Claims.Item : null, createdClaim.Data);
            Assert.Equal(success ? "Success" : fixture.Claims.CreateResult.Error, createdClaim.Message);
            var updatedClaim = await fixture.Sender.Send(new UpdateClaimCommand(fixture.Claims.Item.Id, fixture.Claims.UpdateDto), cancellationToken);
            Assert.Equal(success, updatedClaim.Success);
            Assert.Equal(success ? 200 : 400, updatedClaim.StatusCode);
            Assert.Null(updatedClaim.Data);
            Assert.Equal(success ? "Success" : fixture.Claims.WriteResult.Error, updatedClaim.Message);
            var deletedClaim = await fixture.Sender.Send(new DeleteClaimCommand(fixture.Claims.Item.Id), cancellationToken);
            Assert.Equal(success, deletedClaim.Success);
            Assert.Equal(success ? 204 : 400, deletedClaim.StatusCode);
            Assert.Null(deletedClaim.Data);
            Assert.Equal(success ? "Success" : fixture.Claims.WriteResult.Error, deletedClaim.Message);
            AssertAudits(fixture.Queue, success, EntityNameEnum.Claim, fixture.Claims.Item.Id);
            fixture.Queue.Items.Clear();

            var createdCover = await fixture.Sender.Send(new CreateCoverCommand(fixture.Covers.CreateDto), cancellationToken);
            Assert.Equal(success, createdCover.Success);
            Assert.Equal(success ? 201 : 400, createdCover.StatusCode);
            Assert.Same(success ? fixture.Covers.Item : null, createdCover.Data);
            Assert.Equal(success ? "Success" : fixture.Covers.WriteResult.Error, createdCover.Message);
            var updatedCover = await fixture.Sender.Send(new UpdateCoverCommand(fixture.Covers.Item.Id, fixture.Covers.UpdateDto), cancellationToken);
            Assert.Equal(success, updatedCover.Success);
            Assert.Equal(success ? 200 : 400, updatedCover.StatusCode);
            Assert.Null(updatedCover.Data);
            Assert.Equal(success ? "Success" : fixture.Covers.WriteResult.Error, updatedCover.Message);
            var deletedCover = await fixture.Sender.Send(new DeleteCoverCommand(fixture.Covers.Item.Id), cancellationToken);
            Assert.Equal(success, deletedCover.Success);
            Assert.Equal(success ? 204 : 400, deletedCover.StatusCode);
            Assert.Null(deletedCover.Data);
            Assert.Equal(success ? "Success" : fixture.Covers.WriteResult.Error, deletedCover.Message);
            AssertAudits(fixture.Queue, success, EntityNameEnum.Cover, fixture.Covers.Item.Id);

            Assert.Equal(5, fixture.Claims.Calls);
            Assert.Equal(5, fixture.Covers.Calls);
            Assert.Equal(1, fixture.Premium.Calls);
            Assert.All(fixture.Queue.Tokens, token => Assert.Equal(cancellationToken, token));
        }

        private static void AssertAudits(RecordingQueue queue, bool success, EntityNameEnum entity, string id)
        {
            Assert.Equal(success ? 3 : 0, queue.Items.Count);
            if (!success)
                return;

            Assert.Equal(new[] { HttpRequestTypeEnum.POST, HttpRequestTypeEnum.PUT, HttpRequestTypeEnum.DELETE },
                queue.Items.Select(item => item.Type));
            Assert.All(queue.Items, item =>
            {
                Assert.Equal(entity, item.EntityName);
                Assert.Equal(id, item.EntityId);
            });
        }

        private sealed class Fixture : IDisposable
        {
            private readonly ServiceProvider provider;
            public StubClaimService Claims { get; }
            public StubCoverService Covers { get; }
            public StubPremiumService Premium { get; } = new();
            public RecordingQueue Queue { get; } = new();
            public ISender Sender { get; }

            public Fixture(bool success)
            {
                Claims = new StubClaimService(success);
                Covers = new StubCoverService(success);
                var services = new ServiceCollection();
                RegisterServices(services);
                provider = services.BuildServiceProvider();
                Sender = provider.GetRequiredService<ISender>();
            }

            public void RegisterServices(IServiceCollection services)
            {
                services.AddLogging();
                services.AddApplicationServices();
                services.AddSingleton<IClaimService>(Claims);
                services.AddSingleton<ICoverService>(Covers);
                services.AddSingleton<IPremiumCalculationService>(Premium);
                services.AddSingleton<IChannelQueue<AuditLog>>(Queue);
            }

            public void Dispose() => provider.Dispose();
        }

        private sealed class StubClaimService(bool success) : IClaimService
        {
            public Claim Item { get; } = new() { CoverId = "cover-id", Name = "claim" };
            public IEnumerable<Claim> Items { get; } = new[] { new Claim { CoverId = "cover-id", Name = "listed claim" } };
            public CreateClaimDto CreateDto { get; } = new("cover-id", "claim", ClaimType.Fire, 100m, DateTime.Today);
            public UpdateClaimDto UpdateDto { get; } = new("updated claim", ClaimType.Collision, 200m);
            public ServiceResponseModel<Claim?> CreateResult => createResult ??= success
                ? ServiceResponseModel<Claim?>.SuccessResult(Item)
                : ServiceResponseModel<Claim?>.FailureResult("creation failed");
            private ServiceResponseModel<Claim?>? createResult;
            public ServiceResponseModel<Claim> WriteResult => success
                ? writeResult ??= ServiceResponseModel<Claim>.SuccessResult(Item)
                : writeResult ??= ServiceResponseModel<Claim>.FailureResult("write failed");
            private ServiceResponseModel<Claim>? writeResult;
            public int Calls { get; private set; }

            public Task<IEnumerable<Claim>> GetClaimsAsync() { Calls++; return Task.FromResult(Items); }
            public Task<Claim?> GetClaimAsync(string id) { Calls++; Assert.Equal(Item.Id, id); return Task.FromResult(success ? Item : null); }
            public Task<ServiceResponseModel<Claim?>> CreateClaimAsync(CreateClaimDto item)
            {
                Calls++;
                Assert.Equal(CreateDto, item);
                return Task.FromResult(CreateResult);
            }
            public Task<ServiceResponseModel<Claim>> UpdateClaimAsync(string id, UpdateClaimDto item)
            {
                Calls++;
                Assert.Equal(Item.Id, id);
                Assert.Equal(UpdateDto, item);
                return Task.FromResult(WriteResult);
            }
            public Task<ServiceResponseModel<Claim>> DeleteClaimAsync(string id) { Calls++; Assert.Equal(Item.Id, id); return Task.FromResult(WriteResult); }
        }

        private sealed class StubCoverService(bool success) : ICoverService
        {
            public Cover Item { get; } = new();
            public IEnumerable<Cover> Items { get; } = new[] { new Cover() };
            public CreateCoverDto CreateDto { get; } = new(DateTime.Today, DateTime.Today.AddDays(30), CoverType.Yacht);
            public UpdateCoverDto UpdateDto { get; } = new(DateTime.Today, DateTime.Today.AddDays(60), CoverType.Tanker);
            public ServiceResponseModel<Cover> WriteResult => success
                ? writeResult ??= ServiceResponseModel<Cover>.SuccessResult(Item)
                : writeResult ??= ServiceResponseModel<Cover>.FailureResult("write failed");
            private ServiceResponseModel<Cover>? writeResult;
            public int Calls { get; private set; }

            public Task<IEnumerable<Cover>> GetCoversAsync() { Calls++; return Task.FromResult(Items); }
            public Task<Cover?> GetCoverAsync(string id) { Calls++; Assert.Equal(Item.Id, id); return Task.FromResult(success ? Item : null); }
            public Task<ServiceResponseModel<Cover>> CreateCoverAsync(CreateCoverDto cover) { Calls++; Assert.Equal(CreateDto, cover); return Task.FromResult(WriteResult); }
            public Task<ServiceResponseModel<Cover>> UpdateCoverAsync(string id, UpdateCoverDto item)
            {
                Calls++;
                Assert.Equal(Item.Id, id);
                Assert.Equal(UpdateDto, item);
                return Task.FromResult(WriteResult);
            }
            public Task<ServiceResponseModel<Cover>> DeleteCoverAsync(string id) { Calls++; Assert.Equal(Item.Id, id); return Task.FromResult(WriteResult); }
        }

        private sealed class StubPremiumService : IPremiumCalculationService
        {
            public int Calls { get; private set; }
            public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
            {
                Calls++;
                Assert.Equal(DateTime.Today, startDate);
                Assert.Equal(DateTime.Today.AddDays(30), endDate);
                Assert.Equal(CoverType.Yacht, coverType);
                return 123m;
            }
        }

        private sealed class RecordingQueue : IChannelQueue<AuditLog>
        {
            public List<AuditLog> Items { get; } = [];
            public List<CancellationToken> Tokens { get; } = [];
            public ValueTask EnqueueAsync(AuditLog item, CancellationToken cancellationToken = default)
            {
                Items.Add(item);
                Tokens.Add(cancellationToken);
                return ValueTask.CompletedTask;
            }
            public ValueTask<AuditLog> DequeueAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}