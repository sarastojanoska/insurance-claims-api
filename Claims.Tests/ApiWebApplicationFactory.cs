using Application.Abstractions;
using Application.DTOs;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context.AppDbContext;
using Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Claims.Tests;

internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = Guid.NewGuid().ToString();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public RecordingAuditQueue AuditQueue { get; } = new();
    public DateTime CreatedAt { get; } = DateTime.SpecifyKind(DateTime.Today.AddHours(12), DateTimeKind.Utc);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(CreatedAt));
            services.AddSingleton<IChannelQueue<AuditLog>>(AuditQueue);
            var workers = services.Where(service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType == typeof(QueueProcessorService)).ToArray();
            foreach (var worker in workers)
                services.Remove(worker);
        });
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public async Task<ResponseModel<T>> SendAsync<T>(HttpClient client, HttpMethod method, string route,
        HttpStatusCode expectedStatus, object? body = null)
    {
        using var request = new HttpRequestMessage(method, route);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(expectedStatus, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResponseModel<T>>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal((int)expectedStatus, result.StatusCode);
        Assert.Equal(response.IsSuccessStatusCode, result.Success);
        return result;
    }

    public async Task<Cover> CreateCoverAsync(HttpClient client)
    {
        var response = await SendAsync<Cover>(client, HttpMethod.Post, "/api/covers", HttpStatusCode.Created,
            new CreateCoverDto(CreatedAt.Date, CreatedAt.Date.AddDays(30), CoverType.Yacht));
        Assert.NotNull(response.Data);
        return response.Data;
    }

    public async Task SeedAsync<T>(T entity) where T : class
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public void AssertAudits(EntityNameEnum entity, string id, params HttpRequestTypeEnum[] operations)
    {
        var records = AuditQueue.Items.Where(record => record.EntityName == entity).ToArray();
        Assert.Equal(operations, records.Select(record => record.Type));
        Assert.All(records, record => Assert.Equal(id, record.EntityId));
    }

    internal sealed class RecordingAuditQueue : IChannelQueue<AuditLog>
    {
        private readonly ConcurrentQueue<AuditLog> items = new();
        public IReadOnlyCollection<AuditLog> Items => items.ToArray();

        public ValueTask EnqueueAsync(AuditLog item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Enqueue(item);
            return ValueTask.CompletedTask;
        }

        public ValueTask<AuditLog> DequeueAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTime timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(timestamp);
    }
}