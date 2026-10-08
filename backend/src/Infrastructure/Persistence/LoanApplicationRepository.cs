using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class LoanApplicationRepository : ILoanApplicationRepository
{
    private readonly LoanDbContext _context;

    public LoanApplicationRepository(LoanDbContext context)
    {
        _context = context;
    }

    public Task<LoanApplication?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.LoanApplications.SingleOrDefaultAsync(
            application => application.CustomerId == customerId,
            cancellationToken);

    public void Add(LoanApplication application) => _context.LoanApplications.Add(application);
}
