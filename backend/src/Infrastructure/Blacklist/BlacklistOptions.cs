namespace Infrastructure.Blacklist;

public sealed class BlacklistOptions
{
    public const string SectionName = "Blacklist";

    public string[] Ssns { get; set; } = [];
}
