using API.Extensions;
using API.Filters;
using Infrastructure.Context.AppDbContext;
using Infrastructure.Context.AuditContext;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options => options.Filters.Add<ResponseModelStatusCodeFilter>())
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
builder.Services.AddSwaggerGen(options => options.EnableAnnotations());

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

    var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();
    auditContext.Database.Migrate();

    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    appDbContext.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
