using Cysharp.Threading.Tasks;
using System.Threading;

public interface IJob
{
    int ExecutionOrder { get; }

    UniTask ExecuteAsync(CancellationToken cancellationToken = default);
}