namespace PolsanLeave.TestInfrastructure;

/// <summary>
/// Forces a race to happen: each caller waits at the gate until <c>parties</c> callers have arrived, or the
/// timeout passes. Without serialization the callers meet and proceed together; with it, they cannot meet.
/// </summary>
public sealed class RendezvousGate(int parties, TimeSpan timeout)
{
    private readonly TaskCompletionSource _allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrived;

    public async Task ArriveAsync()
    {
        if (Interlocked.Increment(ref _arrived) >= parties)
        {
            _allArrived.TrySetResult();
        }

        await Task.WhenAny(_allArrived.Task, Task.Delay(timeout));
    }
}
