using Domain;
using Domain.Blacklist;
using Microsoft.Extensions.Options;

namespace Infrastructure.Blacklist;

public sealed class ConfigurationBlacklist : IBlacklist
{
    private readonly HashSet<string> _ssns;

    public ConfigurationBlacklist(IOptions<BlacklistOptions> options)
    {
        _ssns = new HashSet<string>(options.Value.Ssns.Select(Ssn.Normalize));
    }

    public bool Contains(string ssn) => _ssns.Contains(Ssn.Normalize(ssn));
}
