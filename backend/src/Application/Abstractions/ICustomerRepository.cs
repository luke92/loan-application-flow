using Domain.Entities;

namespace Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> FindBySsnAsync(string ssn, CancellationToken cancellationToken);

    void Add(Customer customer);
}
