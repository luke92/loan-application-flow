using Domain.RestrictedStates;
using Microsoft.Extensions.Options;

namespace Infrastructure.RestrictedStates;

public sealed class ConfigurationRestrictedStates : IRestrictedStates
{
    private readonly HashSet<string> _states;

    public ConfigurationRestrictedStates(IOptions<EligibilityRulesOptions> options)
    {
        _states = new HashSet<string>(
            options.Value.RestrictedStates.Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool Contains(string state) => _states.Contains(state);
}
