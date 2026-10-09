namespace Domain;

public static class Ssn
{
    /// <summary>
    /// Canonical form used for storage, lookups, blacklist checks and external services:
    /// digits only (e.g. "123456789"), whether the input has dashes or not.
    /// </summary>
    public static string Normalize(string ssn) => new(ssn.Where(char.IsDigit).ToArray());

    /// <summary>
    /// True for 9 digits that don't start with 9 (that range is reserved for ITINs).
    /// Accepts the value with or without dashes.
    /// </summary>
    public static bool IsValid(string? ssn)
    {
        if (ssn is null)
        {
            return false;
        }

        var digits = Normalize(ssn);
        return digits.Length == 9 && digits[0] != '9';
    }
}
