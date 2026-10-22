namespace PolsanLeave.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Success_HasValueAndNoError()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_HasErrorWithCodeAndField()
    {
        Result<int> result = new Error("Leave.Overlap", "StartDate");

        Assert.False(result.IsSuccess);
        Assert.Equal("Leave.Overlap", result.Error.Code);
        Assert.Equal("StartDate", result.Error.Field);
    }

    [Fact]
    public void Value_OnFailure_Throws()
    {
        Result<int> result = new Error("Balance.Insufficient");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
