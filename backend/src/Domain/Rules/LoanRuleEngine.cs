namespace Domain.Rules;

public sealed class LoanRuleEngine
{
    private readonly IEnumerable<IDenyRule> _rules;

    public LoanRuleEngine(IEnumerable<IDenyRule> rules)
    {
        _rules = rules;
    }

    public LoanDecision Decide(LoanApplicationRequest request)
    {
        foreach (var rule in _rules)
        {
            var reason = rule.Evaluate(request);
            if (reason is not null)
            {
                return LoanDecision.Deny(reason);
            }
        }

        return LoanDecision.Approve();
    }
}
