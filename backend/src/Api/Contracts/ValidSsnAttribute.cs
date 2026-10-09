using System.ComponentModel.DataAnnotations;
using Domain;

namespace Api.Contracts;

/// <summary>Rejects SSNs starting with 9 (ITIN range). Format is checked by the regex attribute.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidSsnAttribute : ValidationAttribute
{
    public ValidSsnAttribute() : base("SSN cannot start with 9 (that range is for ITINs).")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string ssn || Ssn.Normalize(ssn).Length != 9 || Ssn.IsValid(ssn);
}
