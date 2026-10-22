using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PolsanLeave.Balance.Internal.Domain;

namespace PolsanLeave.Balance.Internal.Data;

internal sealed class BalanceDbContext(DbContextOptions<BalanceDbContext> options) : DbContext(options)
{
    public const string Schema = "balance";

    public DbSet<BalanceAdjustment> Adjustments => Set<BalanceAdjustment>();

    public DbSet<BalanceSettings> Settings => Set<BalanceSettings>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<BalanceAdjustment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.EmployeeId);
            e.Property(x => x.Reason).HasMaxLength(BalanceAdjustment.MaxReasonLength);
            e.Property(x => x.Kind).HasConversion<int>();
            e.HasIndex(x => x.EmployeeId, "UX_Adjustments_OneOpeningBalancePerEmployee").HasFilter("[Kind] = 1").IsUnique();
        });

        modelBuilder.Entity<BalanceSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.ToTable(t => t.HasCheckConstraint("CK_Settings_Singleton", $"[Id] = {BalanceSettings.SingletonId}"));
        });
    }

    /// <summary>Denetim izi: adjustments are never modified or deleted.</summary>
    private void EnsureAppendOnly()
    {
        if (ChangeTracker.Entries<BalanceAdjustment>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Balance adjustments are append-only; record a reverse adjustment instead.");
        }
    }
}

/// <summary>Design-time factory for <c>dotnet ef migrations</c>; never used at runtime.</summary>
internal sealed class BalanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BalanceDbContext>
{
    public BalanceDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BalanceDbContext>();
        BalanceModule.Configure(builder, "Server=design-time");
        return new BalanceDbContext(builder.Options);
    }
}
