using Cysharp.Threading.Tasks;
using System.Threading;

public abstract class Job : IJob
{
    protected Job(int executionOrder)
    {
        ExecutionOrder = executionOrder;
    }

    public int ExecutionOrder { get; }

    public abstract UniTask ExecuteAsync(CancellationToken cancellationToken = default);
}
