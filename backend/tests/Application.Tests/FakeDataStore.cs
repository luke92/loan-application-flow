using Application.Abstractions;
using Domain.Entities;
using Domain.Events;

namespace Application.Tests;

/// <summary>
/// In-memory test double that mimics the staged-until-SaveChanges semantics of an EF Core
/// DbContext: Add()/Enqueue() only stage data, and it only becomes visible after a successful
/// SaveChangesAsync call, so it can be used to assert the handler's all-or-nothing behavior.
/// </summary>
internal sealed class FakeDataStore : ICustomerRepository, ILoanApplicationRepository, IOutboxWriter, IUnitOfWork
{
    private readonly List<Customer> _pendingCustomers = new();
    private readonly List<LoanApplication> _pendingApplications = new();
    private readonly List<(CustomerEventType EventType, ExternalCustomerPayload Payload)> _pendingOutboxMessages = new();

    public List<Customer> Customers { get; } = new();
    public List<LoanApplication> Applications { get; } = new();
    public List<(CustomerEventType EventType, ExternalCustomerPayload Payload)> OutboxMessages { get; } = new();

    public bool ThrowOnSaveChanges { get; set; }

    public Task<Customer?> FindBySsnAsync(string ssn, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.SingleOrDefault(customer => customer.Ssn == ssn));

    public void Add(Customer customer) => _pendingCustomers.Add(customer);

    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(Applications.SingleOrDefault(application => application.CustomerId == customerId));

    public void Add(LoanApplication application) => _pendingApplications.Add(application);

    public void Enqueue(CustomerEventType eventType, ExternalCustomerPayload payload) =>
        _pendingOutboxMessages.Add((eventType, payload));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowOnSaveChanges)
        {
            _pendingCustomers.Clear();
            _pendingApplications.Clear();
            _pendingOutboxMessages.Clear();
            throw new InvalidOperationException("Simulated database failure.");
        }

        Customers.AddRange(_pendingCustomers);
        Applications.AddRange(_pendingApplications);
        OutboxMessages.AddRange(_pendingOutboxMessages);

        _pendingCustomers.Clear();
        _pendingApplications.Clear();
        _pendingOutboxMessages.Clear();

        return Task.CompletedTask;
    }
}
