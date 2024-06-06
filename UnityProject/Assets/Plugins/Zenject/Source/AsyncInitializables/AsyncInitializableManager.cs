using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModestTree;
using ModestTree.Util;

namespace Zenject
{
    public class AsyncInitializableManager : IInitializable, IDisposable
    {
        private readonly List<IAsyncInitializable> _initializables;
        private readonly List<ValuePair<Type, int>> _priorities;
        private CancellationTokenSource _lifetimeCts;

        public AsyncInitializableManager(
            [Inject(Optional = true, Source = InjectSources.Local)] List<IAsyncInitializable> initializables,
            [Inject(Optional = true, Source = InjectSources.Local)] List<ValuePair<Type, int>> priorities)
        {
            _initializables = initializables;
            _priorities = priorities;
        }

        public bool IsInitialized { get; private set; }

        public void Dispose()
        {
            IsInitialized = false;
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }

        public void Initialize()
        {
            Assert.IsNull(_lifetimeCts);
            _lifetimeCts = new CancellationTokenSource();

            var initializableInfos = CreateInitializableInfos();
            InitializeAsync(initializableInfos)
                .ContinueWith(() => IsInitialized = true)
                .Forget();
        }

        private IEnumerable<InitializableInfo> CreateInitializableInfos()
        {
            var initializableInfos = _initializables
                .Select(
                    initializable =>
                    {
                        // Note that we use zero for unspecified priority
                        // This is nice because you can use negative or positive for before/after unspecified
                        var initializableType = initializable.GetType();
                        var matches = _priorities.Where(x => initializableType.DerivesFromOrEqual(x.First)).Select(x => x.Second).ToList();
                        var priority = matches.IsEmpty() ? 0 : matches.Distinct().Single();

                        return new InitializableInfo(initializable, priority);
                    })
                .OrderBy(x => x.Priority)
                .ToList();

#if UNITY_EDITOR
            foreach (var initializable in initializableInfos.Select(x => x.Initializable).GetDuplicates())
            {
                Assert.That(false, $"Found duplicate {typeof(IAsyncInitializable)}, type=\"{initializable.GetType()}\"");
            }
#endif

            return initializableInfos;
        }

        private async UniTask InitializeAsync(IEnumerable<InitializableInfo> initializableInfos)
        {
            var groups = initializableInfos.GroupBy(x => x.Priority);
            foreach (var initializables in groups.OrderBy(x => x.Key))
            {
                var types = string.Join(",", initializables.Select(x => x.Initializable.GetType().Name));

                try
                {
                    await UniTask.WhenAll(initializables.Select(x => x.Initializable.InitializeAsync(_lifetimeCts.Token)));
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    throw Assert.CreateException(e, $"Error occurred while initializing {typeof(IAsyncInitializable)}, types=\"{types}\" error=\"{e.Message}\"");
                }
            }
        }

        private class InitializableInfo
        {
            public InitializableInfo(IAsyncInitializable initializable, int priority)
            {
                Initializable = initializable;
                Priority = priority;
            }

            public IAsyncInitializable Initializable { get; }
            public int Priority { get; }
        }
    }
}