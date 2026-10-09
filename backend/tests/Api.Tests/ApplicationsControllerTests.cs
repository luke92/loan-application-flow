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

    private static object ValidPayload(string state = "CA", string ssn = "123-45-6789", decimal amount = 1000m, string zip = "12345") =>
        new
        {
            firstName = "Jane",
            lastName = "Doe",
            street = "1 Main St",
            city = "Springfield",
            state,
            zip,
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
        var response = await _client.PostAsJsonAsync("/api/applications", ValidPayload(ssn: "777-77-7777"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal("Denied", body!.Status);
    }

    [Fact]
    public async Task Submit_WhenBlacklistedSsnHasNoDashes_ReturnsDenied()
    {
        var response = await _client.PostAsJsonAsync("/api/applications", ValidPayload(ssn: "777777777"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal("Denied", body!.Status);
    }

    [Fact]
    public async Task Submit_WhenSameSsnWithAndWithoutDashes_UpdatesSameCustomer()
    {
        var first = await _client.PostAsJsonAsync("/api/applications", ValidPayload(ssn: "444-44-4444"));
        var second = await _client.PostAsJsonAsync("/api/applications", ValidPayload(ssn: "444444444", amount: 2000m));

        var firstBody = await first.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<SubmitLoanApplicationResponse>();

        Assert.Equal(firstBody!.ApplicationId, secondBody!.ApplicationId);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
        var customer = await context.Customers.SingleAsync(c => c.Ssn == "444444444");
        Assert.Equal("444444444", customer.Ssn);
    }

    [Theory]
    [InlineData("ca", "555-55-0001")]
    [InlineData("Ca", "555-55-0002")]
    [InlineData("CA", "555-55-0003")]
    public async Task Submit_WhenStateHasAnyCasing_StoresItUppercase(string state, string ssn)
    {
        var response = await _client.PostAsJsonAsync("/api/applications", ValidPayload(state: state, ssn: ssn));
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
        var normalizedSsn = ssn.Replace("-", "");
        var customer = await context.Customers.SingleAsync(c => c.Ssn == normalizedSsn);

        Assert.Equal("CA", customer.State);
    }

    [Theory]
    [InlineData("ZZ", "12345", "123-45-6789")]
    [InlineData("C", "12345", "123-45-6789")]
    [InlineData("CA", "1234", "123-45-6789")]
    [InlineData("CA", "12345-678", "123-45-6789")]
    [InlineData("CA", "12345", "123-45-678")]
    [InlineData("CA", "12345", "923-45-6789")]
    [InlineData("CA", "12345", "923456789")]
    public async Task Submit_WhenStateZipOrSsnIsInvalid_ReturnsBadRequest(string state, string zip, string ssn)
    {
        var response = await _client.PostAsJsonAsync("/api/applications", ValidPayload(state: state, ssn: ssn, zip: zip));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
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

        Assert.Equal(1, await context.Customers.CountAsync(customer => customer.Ssn == "333333333"));

        var customerId = await context.Customers
            .Where(customer => customer.Ssn == "333333333")
            .Select(customer => customer.Id)
            .SingleAsync();
        Assert.Equal(1, await context.LoanApplications.CountAsync(a => a.CustomerId == customerId));

        var application = await context.LoanApplications.SingleAsync(a => a.Id == secondBody!.ApplicationId);
        Assert.Equal(3000m, application.RequestedAmount);
    }
}
