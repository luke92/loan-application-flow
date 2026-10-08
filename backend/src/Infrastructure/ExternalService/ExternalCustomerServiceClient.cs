using System.Net.Http.Json;
using Application.Abstractions;

namespace Infrastructure.ExternalService;

public sealed class ExternalCustomerServiceClient : IExternalCustomerService
{
    private readonly HttpClient _httpClient;

    public ExternalCustomerServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task NotifyNewCustomerAsync(ExternalCustomerPayload payload, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/customers", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task NotifyUpdatedCustomerAsync(ExternalCustomerPayload payload, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PutAsJsonAsync($"/customers/{payload.Ssn}", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
