using System.Text.Json;
using Application.Abstractions;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests;

public class OutboxProcessorTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"outbox_tests_{Guid.NewGuid():N}.db");

    private ServiceProvider BuildProvider(IExternalCustomerService externalService)
    {
        var services = new ServiceCollection();
        services.AddDbContext<LoanDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        services.AddSingleton(externalService);
        return services.BuildServiceProvider();
    }

    private static ExternalCustomerPayload SamplePayload() => new(
        CustomerId: Guid.NewGuid(),
        Ssn: "123-45-6789",
        FirstName: "Jane",
        LastName: "Doe",
        Street: "1 Main St",
        City: "Springfield",
        State: "CA",
        Zip: "12345",
        CompanyName: "Acme Inc",
        ApplicationId: Guid.NewGuid(),
        RequestedAmount: 1000m);

    [Fact]
    public async Task ExecuteAsync_WhenDeliverySucceeds_MarksMessageProcessed()
    {
        var externalService = new FakeExternalCustomerService();
        using var provider = BuildProvider(externalService);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.OutboxMessages.Add(new OutboxMessage("Created", JsonSerializer.Serialize(SamplePayload())));
            await context.SaveChangesAsync();
        }

        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxProcessorOptions { PollingIntervalSeconds = 60, MaxAttempts = 5 }),
            NullLogger<OutboxProcessor>.Instance);

        await processor.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => externalService.CallCount >= 1);
        await processor.StopAsync(CancellationToken.None);

        using var assertScope = provider.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<LoanDbContext>();
        var message = await assertContext.OutboxMessages.SingleAsync();

        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.Single(externalService.NewCustomerCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeliveryKeepsFailing_RetriesThenMarksFailed()
    {
        var externalService = new FakeExternalCustomerService { ShouldThrow = true };
        using var provider = BuildProvider(externalService);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.OutboxMessages.Add(new OutboxMessage("Created", JsonSerializer.Serialize(SamplePayload())));
            await context.SaveChangesAsync();
        }

        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxProcessorOptions { PollingIntervalSeconds = 1, MaxAttempts = 2 }),
            NullLogger<OutboxProcessor>.Instance);

        await processor.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => externalService.CallCount >= 2, TimeSpan.FromSeconds(10));
        await processor.StopAsync(CancellationToken.None);

        using var assertScope = provider.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<LoanDbContext>();
        var message = await assertContext.OutboxMessages.SingleAsync();

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Equal(2, message.Attempts);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    public void Dispose()
    {
        foreach (var path in new[] { _dbPath, $"{_dbPath}-wal", $"{_dbPath}-shm" })
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
