using Domain.Blacklist;
using Domain.Rules;

namespace Domain.Tests;

public class LoanRuleEngineTests
{
    private static LoanApplicationRequest ValidRequest(string state = "CA", string ssn = "123-45-6789") =>
        new(
            FirstName: "Jane",
            LastName: "Doe",
            Street: "1 Main St",
            City: "Springfield",
            State: state,
            Zip: "12345",
            CompanyName: "Acme Inc",
            RequestedAmount: 1000m,
            Ssn: ssn);

    private sealed class FakeBlacklist : IBlacklist
    {
        private readonly HashSet<string> _ssns;

        public FakeBlacklist(params string[] ssns) => _ssns = new HashSet<string>(ssns);

        public bool Contains(string ssn) => _ssns.Contains(ssn);
    }

    private sealed class NeverDenyRule : IDenyRule
    {
        public DenyReason? Evaluate(LoanApplicationRequest request) => null;
    }

    [Fact]
    public void Decide_WhenStateIsNewYork_Denies()
    {
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new StateIsNewYorkRule(),
            new BlacklistedSsnRule(new FakeBlacklist())
        });

        var decision = engine.Decide(ValidRequest(state: "NY"));

        Assert.False(decision.IsApproved);
        Assert.Equal("StateNotEligible", decision.Reason!.Code);
    }

    [Fact]
    public void Decide_WhenSsnIsBlacklisted_Denies()
    {
        var blacklistedSsn = "999-99-9999";
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new StateIsNewYorkRule(),
            new BlacklistedSsnRule(new FakeBlacklist(blacklistedSsn))
        });

        var decision = engine.Decide(ValidRequest(ssn: blacklistedSsn));

        Assert.False(decision.IsApproved);
        Assert.Equal("BlacklistedSsn", decision.Reason!.Code);
    }

    [Fact]
    public void Decide_WhenStateIsEligibleAndSsnIsNotBlacklisted_Approves()
    {
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new StateIsNewYorkRule(),
            new BlacklistedSsnRule(new FakeBlacklist("999-99-9999"))
        });

        var decision = engine.Decide(ValidRequest());

        Assert.True(decision.IsApproved);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void Decide_WhenNewRuleIsAdded_ExistingRulesStillEvaluateCorrectly()
    {
        var blacklistedSsn = "999-99-9999";
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new StateIsNewYorkRule(),
            new BlacklistedSsnRule(new FakeBlacklist(blacklistedSsn)),
            new NeverDenyRule()
        });

        Assert.False(engine.Decide(ValidRequest(state: "NY")).IsApproved);
        Assert.False(engine.Decide(ValidRequest(ssn: blacklistedSsn)).IsApproved);
        Assert.True(engine.Decide(ValidRequest()).IsApproved);
    }
}
