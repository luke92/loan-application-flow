namespace Domain.Rules;

public sealed class StateIsNewYorkRule : IDenyRule
{
    public DenyReason? Evaluate(LoanApplicationRequest request)
    {
        if (string.Equals(request.State, "NY", StringComparison.OrdinalIgnoreCase))
        {
            return new DenyReason("StateNotEligible", "Applications from New York are not eligible.");
        }

        return null;
    }
}
