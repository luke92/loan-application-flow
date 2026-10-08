using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly LoanDbContext _context;

    public CustomerRepository(LoanDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> FindBySsnAsync(string ssn, CancellationToken cancellationToken) =>
        _context.Customers.SingleOrDefaultAsync(customer => customer.Ssn == ssn, cancellationToken);

    public void Add(Customer customer) => _context.Customers.Add(customer);
}
