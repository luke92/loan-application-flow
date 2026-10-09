using Domain.RestrictedStates;

namespace Domain.Rules;

public sealed class RestrictedStateRule : IDenyRule
{
    private readonly IRestrictedStates _restrictedStates;

    public RestrictedStateRule(IRestrictedStates restrictedStates)
    {
        _restrictedStates = restrictedStates;
    }

    public DenyReason? Evaluate(LoanApplicationRequest request)
    {
        if (_restrictedStates.Contains(request.State))
        {
            return new DenyReason(
                "StateNotEligible",
                $"Applications from {UsStates.NameOf(request.State)} are not eligible.");
        }

        return null;
    }
}
