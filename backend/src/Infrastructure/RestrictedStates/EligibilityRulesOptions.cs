namespace Infrastructure.RestrictedStates;

public sealed class EligibilityRulesOptions
{
    public const string SectionName = "EligibilityRules";

    public string[] RestrictedStates { get; set; } = [];
}
