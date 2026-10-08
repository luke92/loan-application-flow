namespace Domain.Entities;

public sealed class LoanApplication
{
    public Guid Id { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public Guid CustomerId { get; private set; }

    private LoanApplication()
    {
    }

    public LoanApplication(decimal requestedAmount, Guid customerId)
    {
        Id = Guid.NewGuid();
        RequestedAmount = requestedAmount;
        CustomerId = customerId;
    }

    public void UpdateRequestedAmount(decimal requestedAmount)
    {
        RequestedAmount = requestedAmount;
    }
}
