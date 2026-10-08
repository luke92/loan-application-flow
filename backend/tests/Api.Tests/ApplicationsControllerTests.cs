using System.Net.Http.Json;
using Api.Contracts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class ApplicationsControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public ApplicationsControllerTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static object ValidPayload(string state = "CA", string ssn = "123-45-6789", decimal amount = 1000m) =>
        new
        {
            firstName = "Jane",
            lastName = "Doe",
            street = "1 Main St",
            city = "Springfield",
            state,
            zip = "12345",
            companyName = "Acme Inc",
            requestedAmount = amount,
            ssn
        };

    [Fact]
    public async Task Submit_WhenRequestIsValid_ReturnsApproved()
    {
        var response = await _client.PostAsJsonAsync("/api/applications", ValidPayload(ssn: "111-11-1111"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal("Approved", body!.Status);
        Assert.NotNull(body.ApplicationId);
    }

    [Fact]
    public async Task Submit_WhenStateIsNewYork_ReturnsDenied()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/applications",
            ValidPayload(state: "NY", ssn: "222-22-2222"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal("Denied", body!.Status);
        Assert.Contains("New York", body.Reason);
    }

    [Fact]
    public async Task Submit_WhenSsnIsBlacklisted_ReturnsDenied()
    {
        var response = await _client.PostAsJsonAsync("/api/applications", ValidPayload(ssn: "999-99-9999"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal("Denied", body!.Status);
    }

    [Fact]
    public async Task Submit_WhenSameSsnSubmittedTwice_UpdatesExistingCustomerAndApplication()
    {
        const string ssn = "333-33-3333";

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/applications",
            ValidPayload(ssn: ssn, amount: 1000m));
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/applications",
            ValidPayload(ssn: ssn, amount: 3000m));
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal(firstBody!.ApplicationId, secondBody!.ApplicationId);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();

        Assert.Equal(1, await context.Customers.CountAsync(customer => customer.Ssn == ssn));

        var customerId = await context.Customers
            .Where(customer => customer.Ssn == ssn)
            .Select(customer => customer.Id)
            .SingleAsync();
        Assert.Equal(1, await context.LoanApplications.CountAsync(a => a.CustomerId == customerId));

        var application = await context.LoanApplications.SingleAsync(a => a.Id == secondBody!.ApplicationId);
        Assert.Equal(3000m, application.RequestedAmount);
    }
}
