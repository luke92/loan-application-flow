using Domain.Events;

namespace Application.Abstractions;

public interface IOutboxWriter
{
    void Enqueue(CustomerEventType eventType, ExternalCustomerPayload payload);
}
