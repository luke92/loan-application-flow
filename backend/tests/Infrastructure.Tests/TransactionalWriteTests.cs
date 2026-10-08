using Domain.Entities;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

/// <summary>
/// Proves the real EF Core + SQLite transaction guarantee the outbox relies on:
/// Customer, LoanApplication, and OutboxMessage are written by one SaveChangesAsync
/// call, so if anything in that call fails, nothing from it is persisted — not even
/// otherwise-valid rows staged in the same call. Application.Tests covers the same
/// contract against an in-memory fake; this covers it against a real database.
/// </summary>
public class TransactionalWriteTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tx_tests_{Guid.NewGuid():N}.db");

    private LoanDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddDbContext<LoanDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        return services.BuildServiceProvider().GetRequiredService<LoanDbContext>();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenOneInsertInTheBatchViolatesAConstraint_RollsBackTheWholeBatch()
    {
        using (var seedContext = CreateContext())
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Customers.Add(new Customer(
                "Existing", "Customer", "1 Main St", "Springfield", "CA", "12345", "Acme Inc", "111-11-1111"));
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();

        // A perfectly valid new customer + application + outbox message...
        var validCustomer = new Customer(
            "New", "Applicant", "2 Oak St", "Reno", "NV", "89501", "Oak LLC", "222-22-2222");
        var validApplication = new LoanApplication(1000m, validCustomer.Id);
        var validOutboxMessage = new OutboxMessage("Created", "{}");

        // ...staged in the SAME SaveChanges call as one that violates the unique SSN
        // index (it reuses the SSN seeded above).
        var conflictingCustomer = new Customer(
            "Conflicting", "Ssn", "3 Pine St", "Austin", "TX", "73301", "Pine LLC", "111-11-1111");
        var conflictingApplication = new LoanApplication(2000m, conflictingCustomer.Id);
        var conflictingOutboxMessage = new OutboxMessage("Created", "{}");

        context.Customers.AddRange(validCustomer, conflictingCustomer);
        context.LoanApplications.AddRange(validApplication, conflictingApplication);
        context.OutboxMessages.AddRange(validOutboxMessage, conflictingOutboxMessage);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        using var assertContext = CreateContext();
        Assert.Equal(1, await assertContext.Customers.CountAsync());
        Assert.Empty(await assertContext.LoanApplications.ToListAsync());
        Assert.Empty(await assertContext.OutboxMessages.ToListAsync());
    }

    public void Dispose()
    {
        foreach (var path in new[] { _dbPath, $"{_dbPath}-wal", $"{_dbPath}-shm" })
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
