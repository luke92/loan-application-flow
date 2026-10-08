using Application.Abstractions;

namespace Infrastructure.Tests;

internal sealed class FakeExternalCustomerService : IExternalCustomerService
{
    public int CallCount { get; private set; }
    public bool ShouldThrow { get; set; }
    public List<ExternalCustomerPayload> NewCustomerCalls { get; } = new();
    public List<ExternalCustomerPayload> UpdatedCustomerCalls { get; } = new();

    public Task NotifyNewCustomerAsync(ExternalCustomerPayload payload, CancellationToken cancellationToken)
    {
        CallCount++;
        if (ShouldThrow)
        {
            throw new HttpRequestException("Simulated failure.");
        }

        NewCustomerCalls.Add(payload);
        return Task.CompletedTask;
    }

    public Task NotifyUpdatedCustomerAsync(ExternalCustomerPayload payload, CancellationToken cancellationToken)
    {
        CallCount++;
        if (ShouldThrow)
        {
            throw new HttpRequestException("Simulated failure.");
        }

        UpdatedCustomerCalls.Add(payload);
        return Task.CompletedTask;
    }
}
