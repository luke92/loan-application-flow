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
    string Ssn)
{
    /// <summary>
    /// Canonical form used by rules, persistence and external services:
    /// upper-case state and digits-only SSN.
    /// </summary>
    public LoanApplicationRequest Normalized() =>
        this with { State = State.ToUpperInvariant(), Ssn = Domain.Ssn.Normalize(Ssn) };
}
