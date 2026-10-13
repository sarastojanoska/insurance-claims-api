using API.Extensions;
using Infastructure.Context.AppDbContext;
using Infastructure.Context.AuditContext;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Text.Json.Serialization;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB
//var sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
//        ? new MsSqlBuilder()
//            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
//        : new()

//    ).Build();

//var mongoContainer = new MongoDbBuilder()
//    .WithImage("mongo:latest")
//    .Build();

//await sqlContainer.StartAsync();
//await mongoContainer.StartAsync();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.AddServiceDefaults();

builder.Services.AddDbContext<AuditContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Audit");
    options.UseSqlServer(connectionString);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("InsuranceDb");
    const string databaseName = "InsuranceDb";

    var client = new MongoClient(connectionString);
    options.UseMongoDB(client, databaseName);
});

builder.Services.AddApplicationServices();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    // SQL Server: apply pending EF Core migrations.
    var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();
    auditContext.Database.Migrate();

    // MongoDB: no migrations support, so just ensure collections/indexes exist.
    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    appDbContext.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
