using Microsoft.Extensions.Time.Testing;

namespace PolsanLeave.SharedKernel.Tests;

public class BusinessClockTests
{
    [Fact]
    public void Today_JustBeforeIstanbulMidnight_IsPreviousDay()
    {
        var clock = new BusinessClock(new FakeTimeProvider(new DateTimeOffset(2026, 10, 21, 20, 59, 59, TimeSpan.Zero)));

        Assert.Equal(new DateOnly(2026, 10, 21), clock.Today);
    }

    [Fact]
    public void Today_AtIstanbulMidnight_IsNextDayWhileUtcIsStillPreviousDay()
    {
        var clock = new BusinessClock(new FakeTimeProvider(new DateTimeOffset(2026, 10, 21, 21, 0, 0, TimeSpan.Zero)));

        Assert.Equal(new DateOnly(2026, 10, 22), clock.Today);
    }

    [Fact]
    public void UtcNow_ReturnsTimeProviderValue()
    {
        var now = new DateTimeOffset(2026, 10, 22, 8, 30, 0, TimeSpan.Zero);

        Assert.Equal(now, new BusinessClock(new FakeTimeProvider(now)).UtcNow);
    }
}
