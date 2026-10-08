namespace Domain.Rules;

public interface IDenyRule
{
    DenyReason? Evaluate(LoanApplicationRequest request);
}
