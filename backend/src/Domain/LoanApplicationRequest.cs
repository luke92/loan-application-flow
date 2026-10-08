namespace Domain;

public sealed record LoanApplicationRequest(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string State,
    string Zip,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn);
