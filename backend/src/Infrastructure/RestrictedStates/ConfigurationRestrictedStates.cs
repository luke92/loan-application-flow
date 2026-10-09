using Domain.RestrictedStates;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.RestrictedStates;

public sealed class ConfigurationRestrictedStates : IRestrictedStates
{
    private readonly HashSet<string> _states;

    public ConfigurationRestrictedStates(IConfiguration configuration)
    {
        _states = new HashSet<string>(
            (configuration.GetSection("EligibilityRules:RestrictedStates").Get<string[]>() ?? Array.Empty<string>())
                .Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool Contains(string state) => _states.Contains(state);
}
