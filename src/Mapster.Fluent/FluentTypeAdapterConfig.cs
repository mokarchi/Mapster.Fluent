using System;
using System.Linq.Expressions;

namespace Mapster.Fluent
{
    public class FluentTypeAdapterConfig : IFluentMapperConfig
    {
        private readonly TypeAdapterConfig _innerConfig;

        public FluentTypeAdapterConfig(TypeAdapterConfig innerConfig)
        {
            _innerConfig = innerConfig ?? throw new ArgumentNullException(nameof(innerConfig));
        }

        public ITypeConfig<TSource, TDestination> ForType<TSource, TDestination>()
        {
            return new TypeConfig<TSource, TDestination>(_innerConfig.ForType<TSource, TDestination>());
        }

        public ITypeConfig<TSource, TDestination> NewConfig<TSource, TDestination>()
        {
            return new TypeConfig<TSource, TDestination>(_innerConfig.NewConfig<TSource, TDestination>());
        }

        public TypeAdapterConfig GetInnerConfig() => _innerConfig;

        private class TypeConfig<TSource, TDestination> : ITypeConfig<TSource, TDestination>
        {
            private readonly TypeAdapterSetter<TSource, TDestination> _setter;

            public TypeConfig(TypeAdapterSetter<TSource, TDestination> setter)
            {
                _setter = setter;
            }

            public ITypeConfig<TSource, TDestination> Map<TMember>(
                Expression<Func<TDestination, TMember>> destinationMember,
                Expression<Func<TSource, TMember>> sourceMember)
            {
                _setter.Map(destinationMember, sourceMember);
                return this;
            }

            public ITypeConfig<TSource, TDestination> Ignore(Expression<Func<TDestination, object>> destinationMember)
            {
                _setter.Ignore(destinationMember);
                return this;
            }
        }
    }
}
