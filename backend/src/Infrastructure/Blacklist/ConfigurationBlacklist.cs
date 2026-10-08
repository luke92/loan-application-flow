using Domain.Blacklist;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Blacklist;

public sealed class ConfigurationBlacklist : IBlacklist
{
    private readonly HashSet<string> _ssns;

    public ConfigurationBlacklist(IConfiguration configuration)
    {
        _ssns = new HashSet<string>(
            configuration.GetSection("Blacklist:Ssns").Get<string[]>() ?? Array.Empty<string>());
    }

    public bool Contains(string ssn) => _ssns.Contains(ssn);
}
