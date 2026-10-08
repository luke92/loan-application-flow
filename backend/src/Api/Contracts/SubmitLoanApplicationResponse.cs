namespace Api.Contracts;

public sealed class SubmitLoanApplicationResponse
{
    public string Status { get; init; } = string.Empty;
    public Guid? ApplicationId { get; init; }
    public string? Reason { get; init; }

    public static SubmitLoanApplicationResponse Approved(Guid applicationId) =>
        new() { Status = "Approved", ApplicationId = applicationId };

    public static SubmitLoanApplicationResponse Denied(string reason) =>
        new() { Status = "Denied", Reason = reason };
}
