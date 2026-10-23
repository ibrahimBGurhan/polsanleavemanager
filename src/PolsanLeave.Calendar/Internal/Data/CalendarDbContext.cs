using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PolsanLeave.Calendar.Internal.Domain;

namespace PolsanLeave.Calendar.Internal.Data;

internal sealed class CalendarDbContext(DbContextOptions<CalendarDbContext> options) : DbContext(options)
{
    public const string Schema = "calendar";

    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    public DbSet<CalendarYear> CalendarYears => Set<CalendarYear>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<PublicHoliday>(e =>
        {
            e.HasKey(h => h.Date);
            e.Property(h => h.Name).HasMaxLength(200);
            e.HasData(TurkishPublicHolidays.All);
        });

        modelBuilder.Entity<CalendarYear>(e =>
        {
            e.HasKey(y => y.Year);
            e.Property(y => y.Year).ValueGeneratedNever();
            e.HasData(TurkishPublicHolidays.Years);
        });
    }
}

/// <summary>Design-time factory for <c>dotnet ef migrations</c>; never used at runtime.</summary>
internal sealed class CalendarDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CalendarDbContext>
{
    public CalendarDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<CalendarDbContext>();
        CalendarModule.Configure(builder, "Server=design-time");
        return new CalendarDbContext(builder.Options);
    }
}
