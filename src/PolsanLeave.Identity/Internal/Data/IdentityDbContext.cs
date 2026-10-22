using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PolsanLeave.Identity.Internal.Domain;

namespace PolsanLeave.Identity.Internal.Data;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.EmployeeNumber).HasMaxLength(50);
            e.HasIndex(x => x.EmployeeNumber).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.UserPrincipalName).HasMaxLength(256);
            e.HasIndex(x => x.UserPrincipalName).IsUnique();
            e.Property(x => x.EntraObjectId).HasMaxLength(64);
            e.HasIndex(x => x.EntraObjectId).IsUnique().HasFilter("[EntraObjectId] IS NOT NULL");
            e.HasOne<Employee>().WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

/// <summary>Design-time factory for <c>dotnet ef migrations</c>; never used at runtime.</summary>
internal sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<IdentityDbContext>();
        IdentityModule.Configure(builder, "Server=design-time");
        return new IdentityDbContext(builder.Options);
    }
}
