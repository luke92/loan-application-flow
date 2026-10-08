namespace Application.Abstractions;

public sealed record ExternalCustomerPayload(
    Guid CustomerId,
    string Ssn,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string Zip,
    string CompanyName,
    Guid ApplicationId,
    decimal RequestedAmount);
