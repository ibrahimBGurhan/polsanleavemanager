using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PolsanLeave.Leave.Contracts;
using PolsanLeave.Leave.Internal.Domain;

namespace PolsanLeave.Leave.Internal.Data;

internal sealed class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : DbContext(options)
{
    public const string Schema = "leave";

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Description).HasMaxLength(LeaveErrors.MaxDescriptionLength);
            e.Property(x => x.Status).HasConversion<int>();
            e.Ignore(x => x.IsActive);
            e.HasIndex(x => new { x.EmployeeId, x.Status });
            e.HasIndex(x => x.ApproverId);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_LeaveRequests_DateRange", "[EndDate] >= [StartDate]");
                t.HasCheckConstraint("CK_LeaveRequests_WorkingDays", "[WorkingDays] > 0");
            });
        });
    }
}

/// <summary>Design-time factory for <c>dotnet ef migrations</c>; never used at runtime.</summary>
internal sealed class LeaveDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LeaveDbContext>
{
    public LeaveDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LeaveDbContext>();
        LeaveModule.Configure(builder, "Server=design-time");
        return new LeaveDbContext(builder.Options);
    }
}
