using Domain.Rules;

namespace Application.UseCases;

public sealed class SubmitLoanApplicationResult
{
    public bool IsApproved { get; }
    public Guid? ApplicationId { get; }
    public DenyReason? Reason { get; }

    private SubmitLoanApplicationResult(bool isApproved, Guid? applicationId, DenyReason? reason)
    {
        IsApproved = isApproved;
        ApplicationId = applicationId;
        Reason = reason;
    }

    public static SubmitLoanApplicationResult Approved(Guid applicationId) => new(true, applicationId, null);

    public static SubmitLoanApplicationResult Denied(DenyReason reason) => new(false, null, reason);
}
