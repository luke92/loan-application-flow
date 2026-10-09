using Application.Abstractions;
using Domain;
using Domain.Entities;
using Domain.Events;
using Domain.Rules;

namespace Application.UseCases;

public sealed class SubmitLoanApplicationHandler
{
    private readonly LoanRuleEngine _ruleEngine;
    private readonly ICustomerRepository _customers;
    private readonly ILoanApplicationRepository _applications;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitLoanApplicationHandler(
        LoanRuleEngine ruleEngine,
        ICustomerRepository customers,
        ILoanApplicationRepository applications,
        IOutboxWriter outbox,
        IUnitOfWork unitOfWork)
    {
        _ruleEngine = ruleEngine;
        _customers = customers;
        _applications = applications;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubmitLoanApplicationResult> HandleAsync(
        LoanApplicationRequest request,
        CancellationToken cancellationToken)
    {
        request = request.Normalized();

        var decision = _ruleEngine.Decide(request);
        if (!decision.IsApproved)
        {
            return SubmitLoanApplicationResult.Denied(decision.Reason!);
        }

        var existingCustomer = await _customers.FindBySsnAsync(request.Ssn, cancellationToken);

        Customer customer;
        LoanApplication application;
        CustomerEventType eventType;

        if (existingCustomer is null)
        {
            customer = new Customer(
                request.FirstName,
                request.LastName,
                request.Street,
                request.City,
                request.State,
                request.Zip,
                request.CompanyName,
                request.Ssn);
            application = new LoanApplication(request.RequestedAmount, customer.Id);

            _customers.Add(customer);
            _applications.Add(application);
            eventType = CustomerEventType.Created;
        }
        else
        {
            existingCustomer.UpdateFrom(
                request.FirstName,
                request.LastName,
                request.Street,
                request.City,
                request.State,
                request.Zip,
                request.CompanyName);

            var existingApplication = await _applications.FindByCustomerIdAsync(existingCustomer.Id, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Customer {existingCustomer.Id} has no existing loan application.");
            existingApplication.UpdateRequestedAmount(request.RequestedAmount);

            customer = existingCustomer;
            application = existingApplication;
            eventType = CustomerEventType.Updated;
        }

        var payload = new ExternalCustomerPayload(
            customer.Id,
            customer.Ssn,
            customer.FirstName,
            customer.LastName,
            customer.Street,
            customer.City,
            customer.State,
            customer.Zip,
            customer.CompanyName,
            application.Id,
            application.RequestedAmount);

        _outbox.Enqueue(eventType, payload);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SubmitLoanApplicationResult.Approved(application.Id);
    }
}
