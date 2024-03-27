using System;
using ModestTree;
using ModestTree.Util;

namespace Zenject
{
    public static class AsyncInitializablesExtensions
    {
        public static CopyNonLazyBinder BindAsyncInitializableExecutionOrder<T>(this DiContainer container, int order) where T : IAsyncInitializable
        {
            return container.BindAsyncInitializableExecutionOrder(typeof(T), order);
        }

        public static CopyNonLazyBinder BindAsyncInitializableExecutionOrder(this DiContainer container, Type type, int order)
        {
            Assert.That(type.DerivesFrom<IAsyncInitializable>(), "Expected type '{0}' to derive from IAsyncInitializable", type);
            return container.BindInstance(ValuePair.New(type, order)).WhenInjectedInto<AsyncInitializableManager>();
        }
    }
}