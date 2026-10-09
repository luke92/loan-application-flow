using Domain.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class DenyRuleRegistrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public DenyRuleRegistrationTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public void Container_RegistersTheExpectedDenyRulesInOrder()
    {
        var rules = _factory.Services.GetServices<IDenyRule>().Select(rule => rule.GetType()).ToArray();

        Assert.Equal(new[] { typeof(RestrictedStateRule), typeof(BlacklistedSsnRule) }, rules);
    }
}
