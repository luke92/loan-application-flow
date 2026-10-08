using Domain.Entities;

namespace Application.Abstractions;

public interface ILoanApplicationRepository
{
    Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(LoanApplication application);
}
