using Application.Abstractions;
using Application.Services.AuditService;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging
{
    public class QueueProcessorService(IChannelQueue<AuditLog> auditQueue, IServiceScopeFactory serviceScopeFactory, ILogger<QueueProcessorService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Queue processor service started.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await auditQueue.DequeueAsync(cancellationToken);
                    using var scope = serviceScopeFactory.CreateScope();
                    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                    await auditService.AddAuditLogAsync(workItem);
                }
                catch (OperationCanceledException)
                {
                    // Just shutdown, cancelation token is invoked
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing queue item.");
                }
            }
        }
    }
}
