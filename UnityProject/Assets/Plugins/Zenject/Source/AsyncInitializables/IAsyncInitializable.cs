using System.Threading;
using Cysharp.Threading.Tasks;

namespace Zenject
{
    public interface IAsyncInitializable
    {
        UniTask InitializeAsync(CancellationToken cancellationToken);
    }
}