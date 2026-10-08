using System.Text.Json;
using Application.Abstractions;
using Domain.Events;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error while processing outbox messages.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
        var externalService = scope.ServiceProvider.GetRequiredService<IExternalCustomerService>();

        var pendingMessages = await context.OutboxMessages
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            await ProcessMessageAsync(context, externalService, message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        LoanDbContext context,
        IExternalCustomerService externalService,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<ExternalCustomerPayload>(message.Payload)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} has an empty payload.");

            if (message.EventType == nameof(CustomerEventType.Created))
            {
                await externalService.NotifyNewCustomerAsync(payload, cancellationToken);
            }
            else
            {
                await externalService.NotifyUpdatedCustomerAsync(payload, cancellationToken);
            }

            message.MarkProcessed(DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to deliver outbox message {MessageId}.", message.Id);
            message.RecordFailedAttempt(_options.MaxAttempts);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
