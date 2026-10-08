namespace Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string Zip { get; private set; } = null!;
    public string CompanyName { get; private set; } = null!;
    public string Ssn { get; private set; } = null!;

    private Customer()
    {
    }

    public Customer(
        string firstName,
        string lastName,
        string street,
        string city,
        string state,
        string zip,
        string companyName,
        string ssn)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        State = state;
        Zip = zip;
        CompanyName = companyName;
        Ssn = ssn;
    }

    public void UpdateFrom(
        string firstName,
        string lastName,
        string street,
        string city,
        string state,
        string zip,
        string companyName)
    {
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        State = state;
        Zip = zip;
        CompanyName = companyName;
    }
}
