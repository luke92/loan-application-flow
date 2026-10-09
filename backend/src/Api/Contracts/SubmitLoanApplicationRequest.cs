using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class SubmitLoanApplicationRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Street { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    [ValidUsState]
    public string State { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "ZIP must match ##### or #####-####.")]
    public string Zip { get; set; } = string.Empty;

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Requested amount must be greater than 0.")]
    public decimal RequestedAmount { get; set; }

    [Required]
    [RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "SSN must match ###-##-#### (dashes optional).")]
    [ValidSsn]
    public string Ssn { get; set; } = string.Empty;
}
