using System.ComponentModel.DataAnnotations;
using Domain;

namespace Api.Contracts;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidUsStateAttribute : ValidationAttribute
{
    public ValidUsStateAttribute() : base("State must be a valid 2-letter US state code.")
    {
    }

    public override bool IsValid(object? value) => value is string state && UsStates.IsValid(state);
}
