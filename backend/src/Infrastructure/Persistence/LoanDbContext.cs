using Domain.Entities;
using Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(customer => customer.Id);
            builder.HasIndex(customer => customer.Ssn).IsUnique();
            builder.Property(customer => customer.Ssn).IsRequired();
            builder.Property(customer => customer.FirstName).IsRequired();
            builder.Property(customer => customer.LastName).IsRequired();
            builder.Property(customer => customer.Street).IsRequired();
            builder.Property(customer => customer.City).IsRequired();
            builder.Property(customer => customer.State).IsRequired();
            builder.Property(customer => customer.Zip).IsRequired();
            builder.Property(customer => customer.CompanyName).IsRequired();
        });

        modelBuilder.Entity<LoanApplication>(builder =>
        {
            builder.HasKey(application => application.Id);
            builder.Property(application => application.RequestedAmount).IsRequired();
            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(application => application.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(message => message.Id);
            builder.Property(message => message.EventType).IsRequired();
            builder.Property(message => message.Payload).IsRequired();
        });
    }
}
