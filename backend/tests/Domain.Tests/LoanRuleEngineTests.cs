using Domain.Blacklist;
using Domain.RestrictedStates;
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

    private sealed class FakeRestrictedStates : IRestrictedStates
    {
        private readonly HashSet<string> _states;

        public FakeRestrictedStates(params string[] states) =>
            _states = new HashSet<string>(states, StringComparer.OrdinalIgnoreCase);

        public bool Contains(string state) => _states.Contains(state);
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
            new RestrictedStateRule(new FakeRestrictedStates("NY")),
            new BlacklistedSsnRule(new FakeBlacklist())
        });

        var decision = engine.Decide(ValidRequest(state: "NY"));

        Assert.False(decision.IsApproved);
        Assert.Equal("StateNotEligible", decision.Reason!.Code);
    }

    [Fact]
    public void Decide_WhenStateIsInConfiguredList_Denies()
    {
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new RestrictedStateRule(new FakeRestrictedStates("NY", "fl"))
        });

        Assert.False(engine.Decide(ValidRequest(state: "FL")).IsApproved);
        Assert.False(engine.Decide(ValidRequest(state: "ny")).IsApproved);
        Assert.True(engine.Decide(ValidRequest(state: "CA")).IsApproved);
    }

    [Fact]
    public void Decide_WhenStateIsDenied_MessageUsesFullStateName()
    {
        var engine = new LoanRuleEngine(new IDenyRule[] { new RestrictedStateRule(new FakeRestrictedStates("NY", "ZZ")) });

        Assert.Contains("New York", engine.Decide(ValidRequest(state: "ny")).Reason!.Message);
        Assert.Contains("ZZ", engine.Decide(ValidRequest(state: "zz")).Reason!.Message);
    }

    [Fact]
    public void Decide_WhenSsnIsBlacklisted_Denies()
    {
        var blacklistedSsn = "777-77-7777";
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new RestrictedStateRule(new FakeRestrictedStates("NY")),
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
            new RestrictedStateRule(new FakeRestrictedStates("NY")),
            new BlacklistedSsnRule(new FakeBlacklist("777-77-7777"))
        });

        var decision = engine.Decide(ValidRequest());

        Assert.True(decision.IsApproved);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void Decide_WhenNewRuleIsAdded_ExistingRulesStillEvaluateCorrectly()
    {
        var blacklistedSsn = "777-77-7777";
        var engine = new LoanRuleEngine(new IDenyRule[]
        {
            new RestrictedStateRule(new FakeRestrictedStates("NY")),
            new BlacklistedSsnRule(new FakeBlacklist(blacklistedSsn)),
            new NeverDenyRule()
        });

        Assert.False(engine.Decide(ValidRequest(state: "NY")).IsApproved);
        Assert.False(engine.Decide(ValidRequest(ssn: blacklistedSsn)).IsApproved);
        Assert.True(engine.Decide(ValidRequest()).IsApproved);
    }
}
