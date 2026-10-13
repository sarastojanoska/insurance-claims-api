using Application.Abstractions;
using Application.Services.AuditService;
using Application.Services.ClaimService;
using Application.Services.CoverService;
using Application.Services.PremiumCalculationService;
using Infastructure.Messaging;
using Infrastructure.Messaging;
using Services.AuditService;
using Services.ClaimService;
using Services.CoverService;
using Services.PremiumCalculationService;

namespace API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICoverService, CoverService>();
            services.AddScoped<IClaimService, ClaimService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IPremiumCalculationService, PremiumCalculationService>();

            services.AddSingleton(typeof(IChannelQueue<>), typeof(ChannelQueue<>));

            services.AddHostedService<QueueProcessorService>();

            return services;
        }
    }
}
