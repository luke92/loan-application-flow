using Api.Contracts;
using Application.UseCases;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly SubmitLoanApplicationHandler _handler;

    public ApplicationsController(SubmitLoanApplicationHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<ActionResult<SubmitLoanApplicationResponse>> Submit(
        SubmitLoanApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var domainRequest = new LoanApplicationRequest(
            request.FirstName,
            request.LastName,
            request.Street,
            request.City,
            request.State.ToUpperInvariant(),
            request.Zip,
            request.CompanyName,
            request.RequestedAmount,
            request.Ssn);

        var result = await _handler.HandleAsync(domainRequest, cancellationToken);

        return result.IsApproved
            ? Ok(SubmitLoanApplicationResponse.Approved(result.ApplicationId!.Value))
            : Ok(SubmitLoanApplicationResponse.Denied(result.Reason!.Message));
    }
}
