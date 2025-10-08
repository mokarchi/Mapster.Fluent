using System;

namespace Mapster.Fluent
{
    public class DefaultMapContextFactory : IMapContextFactory
    {
        private const string DI_KEY = "Mapster.DependencyInjection.sp";

        public IDisposable CreateScope(IServiceProvider serviceProvider)
        {
            var scope = new MapContextScope();
            scope.Context.Parameters[DI_KEY] = serviceProvider;
            return scope;
        }
    }
}
