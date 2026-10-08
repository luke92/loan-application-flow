namespace Domain.Rules;

public sealed class LoanDecision
{
    public bool IsApproved { get; }
    public DenyReason? Reason { get; }

    private LoanDecision(bool isApproved, DenyReason? reason)
    {
        IsApproved = isApproved;
        Reason = reason;
    }

    public static LoanDecision Approve() => new(true, null);

    public static LoanDecision Deny(DenyReason reason) => new(false, reason);
}
