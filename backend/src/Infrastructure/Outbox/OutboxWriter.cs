using System.Text.Json;
using Application.Abstractions;
using Domain.Events;
using Infrastructure.Persistence;

namespace Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly LoanDbContext _context;

    public OutboxWriter(LoanDbContext context)
    {
        _context = context;
    }

    public void Enqueue(CustomerEventType eventType, ExternalCustomerPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var message = new OutboxMessage(eventType.ToString(), json);
        _context.OutboxMessages.Add(message);
    }
}
