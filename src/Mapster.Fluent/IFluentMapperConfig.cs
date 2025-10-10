using System;
using System.Linq.Expressions;

namespace Mapster.Fluent
{
    public interface IFluentMapperConfig
    {
        ITypeConfig<TSource, TDestination> ForType<TSource, TDestination>();
        ITypeConfig<TSource, TDestination> NewConfig<TSource, TDestination>();
        TypeAdapterConfig GetInnerConfig();
    }

    public interface ITypeConfig<TSource, TDestination>
    {
        ITypeConfig<TSource, TDestination> Map<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Expression<Func<TSource, TMember>> sourceMember);
        ITypeConfig<TSource, TDestination> Ignore(Expression<Func<TDestination, object>> destinationMember);
    }
}
