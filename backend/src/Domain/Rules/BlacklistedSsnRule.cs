using Domain.Blacklist;

namespace Domain.Rules;

public sealed class BlacklistedSsnRule : IDenyRule
{
    private readonly IBlacklist _blacklist;

    public BlacklistedSsnRule(IBlacklist blacklist)
    {
        _blacklist = blacklist;
    }

    public DenyReason? Evaluate(LoanApplicationRequest request)
    {
        if (_blacklist.Contains(request.Ssn))
        {
            return new DenyReason("BlacklistedSsn", "This SSN is not eligible for a loan.");
        }

        return null;
    }
}
