namespace Application.Abstractions;

public interface IExternalCustomerService
{
    Task NotifyNewCustomerAsync(ExternalCustomerPayload payload, CancellationToken cancellationToken);

    Task NotifyUpdatedCustomerAsync(ExternalCustomerPayload payload, CancellationToken cancellationToken);
}
