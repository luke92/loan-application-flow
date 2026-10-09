using Application.UseCases;
using Domain;
using Domain.Blacklist;
using Domain.Events;
using Domain.RestrictedStates;
using Domain.Rules;

namespace Application.Tests;

public class SubmitLoanApplicationHandlerTests
{
    private const string BlacklistedSsn = "777-77-7777";

    private sealed class FakeBlacklist : IBlacklist
    {
        public bool Contains(string ssn) => ssn == BlacklistedSsn;
    }

    private sealed class FakeRestrictedStates : IRestrictedStates
    {
        private readonly HashSet<string> _states;

        public FakeRestrictedStates(params string[] states) =>
            _states = new HashSet<string>(states, StringComparer.OrdinalIgnoreCase);

        public bool Contains(string state) => _states.Contains(state);
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
            new LoanRuleEngine(new IDenyRule[] { new RestrictedStateRule(new FakeRestrictedStates("NY")), new BlacklistedSsnRule(new FakeBlacklist()) }),
            store,
            store,
            store,
            store);

    [Fact]
    public async Task HandleAsync_WhenStateAndSsnAreNotCanonical_StoresThemNormalized()
    {
        var store = new FakeDataStore();
        var handler = CreateHandler(store);

        await handler.HandleAsync(ValidRequest(state: "ca", ssn: "123-45-6789"), CancellationToken.None);

        var customer = Assert.Single(store.Customers);
        Assert.Equal("CA", customer.State);
        Assert.Equal("123456789", customer.Ssn);
    }

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
