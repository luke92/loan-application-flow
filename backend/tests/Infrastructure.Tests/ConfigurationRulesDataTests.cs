using Infrastructure.Blacklist;
using Infrastructure.RestrictedStates;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests;

public class ConfigurationRulesDataTests
{
    [Fact]
    public void RestrictedStates_MatchesCaseInsensitivelyAndTrimsEntries()
    {
        var states = new ConfigurationRestrictedStates(
            Options.Create(new EligibilityRulesOptions { RestrictedStates = ["NY", " fl "] }));

        Assert.True(states.Contains("ny"));
        Assert.True(states.Contains("FL"));
        Assert.False(states.Contains("CA"));
    }

    [Fact]
    public void RestrictedStates_WhenNothingConfigured_RestrictsNothing()
    {
        var states = new ConfigurationRestrictedStates(Options.Create(new EligibilityRulesOptions()));

        Assert.False(states.Contains("NY"));
    }

    [Theory]
    [InlineData("777-77-7777")]
    [InlineData("777777777")]
    public void Blacklist_MatchesRegardlessOfDashesOnEitherSide(string configured)
    {
        var blacklist = new ConfigurationBlacklist(Options.Create(new BlacklistOptions { Ssns = [configured] }));

        Assert.True(blacklist.Contains("777-77-7777"));
        Assert.True(blacklist.Contains("777777777"));
        Assert.False(blacklist.Contains("123-45-6789"));
    }
}
