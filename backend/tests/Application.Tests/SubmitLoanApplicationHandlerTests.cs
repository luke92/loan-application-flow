using Application.UseCases;
using Domain;
using Domain.Blacklist;
using Domain.Events;
using Domain.Rules;

namespace Application.Tests;

public class SubmitLoanApplicationHandlerTests
{
    private const string BlacklistedSsn = "999-99-9999";

    private sealed class FakeBlacklist : IBlacklist
    {
        public bool Contains(string ssn) => ssn == BlacklistedSsn;
    }

    private static LoanApplicationRequest ValidRequest(
        string state = "CA",
        string ssn = "123-45-6789",
        decimal amount = 1000m) =>
        new(
            FirstName: "Jane",
            LastName: "Doe",
            Street: "1 Main St",
            City: "Springfield",
            State: state,
            Zip: "12345",
            CompanyName: "Acme Inc",
            RequestedAmount: amount,
            Ssn: ssn);

    private static SubmitLoanApplicationHandler CreateHandler(FakeDataStore store) =>
        new(
            new LoanRuleEngine(new IDenyRule[] { new StateIsNewYorkRule(), new BlacklistedSsnRule(new FakeBlacklist()) }),
            store,
            store,
            store,
            store);

    [Fact]
    public async Task HandleAsync_WhenNewCustomer_CreatesCustomerAndApplicationAndEnqueuesCreatedEvent()
    {
        var store = new FakeDataStore();
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(ValidRequest(), CancellationToken.None);

        Assert.True(result.IsApproved);
        Assert.Single(store.Customers);
        Assert.Single(store.Applications);
        Assert.Single(store.OutboxMessages);
        Assert.Equal(CustomerEventType.Created, store.OutboxMessages[0].EventType);
    }

    [Fact]
    public async Task HandleAsync_WhenSameSsnSubmittedTwice_UpdatesSameCustomerAndApplication()
    {
        var store = new FakeDataStore();
        var handler = CreateHandler(store);
        const string ssn = "123-45-6789";

        var firstResult = await handler.HandleAsync(ValidRequest(ssn: ssn, amount: 1000m), CancellationToken.None);
        var secondResult = await handler.HandleAsync(ValidRequest(ssn: ssn, amount: 2500m), CancellationToken.None);

        Assert.True(firstResult.IsApproved);
        Assert.True(secondResult.IsApproved);
        Assert.Equal(firstResult.ApplicationId, secondResult.ApplicationId);

        Assert.Single(store.Customers);
        Assert.Single(store.Applications);
        Assert.Equal(2500m, store.Applications[0].RequestedAmount);

        Assert.Equal(2, store.OutboxMessages.Count);
        Assert.Equal(CustomerEventType.Created, store.OutboxMessages[0].EventType);
        Assert.Equal(CustomerEventType.Updated, store.OutboxMessages[1].EventType);
    }

    [Fact]
    public async Task HandleAsync_WhenDenied_PersistsNothing()
    {
        var store = new FakeDataStore();
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(ValidRequest(state: "NY"), CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.NotNull(result.Reason);
        Assert.Empty(store.Customers);
        Assert.Empty(store.Applications);
        Assert.Empty(store.OutboxMessages);
    }

    [Fact]
    public async Task HandleAsync_WhenSaveChangesFails_RollsBackEverything()
    {
        var store = new FakeDataStore { ThrowOnSaveChanges = true };
        var handler = CreateHandler(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(ValidRequest(), CancellationToken.None));

        Assert.Empty(store.Customers);
        Assert.Empty(store.Applications);
        Assert.Empty(store.OutboxMessages);
    }
}
