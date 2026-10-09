using Application.UseCases;
using Domain.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Deny rules, in evaluation order (the first one that denies wins).
        // Adding a rule = one line here.
        services.AddSingleton<IDenyRule, RestrictedStateRule>();
        services.AddSingleton<IDenyRule, BlacklistedSsnRule>();
        services.AddSingleton<LoanRuleEngine>();

        services.AddScoped<SubmitLoanApplicationHandler>();

        return services;
    }
}
