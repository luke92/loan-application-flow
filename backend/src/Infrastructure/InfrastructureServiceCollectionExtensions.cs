using Application.Abstractions;
using Domain.Blacklist;
using Domain.RestrictedStates;
using Infrastructure.Blacklist;
using Infrastructure.ExternalService;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Infrastructure.RestrictedStates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read lazily (not here) so configuration overrides applied after service
        // registration, e.g. by WebApplicationFactory in tests, are honored.
        services.AddDbContext<LoanDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings:Default' is not configured.");
            }

            options.UseSqlite(connectionString);
        });

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.Configure<BlacklistOptions>(configuration.GetSection(BlacklistOptions.SectionName));
        services.Configure<EligibilityRulesOptions>(
            configuration.GetSection(EligibilityRulesOptions.SectionName));

        services.AddSingleton<IBlacklist, ConfigurationBlacklist>();
        services.AddSingleton<IRestrictedStates, ConfigurationRestrictedStates>();

        services.Configure<OutboxProcessorOptions>(
            configuration.GetSection(OutboxProcessorOptions.SectionName));
        services.AddHostedService<OutboxProcessor>();

        services.AddHttpClient<IExternalCustomerService, ExternalCustomerServiceClient>(client =>
        {
            var baseUrl = configuration["ExternalService:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        });

        return services;
    }
}
