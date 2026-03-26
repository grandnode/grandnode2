using System.Linq.Expressions;

namespace Grand.Mapping;

public interface IMappingExpression<TSource, TDest>
{
    IMappingExpression<TSource, TDest> ForMember<TMember>(
        Expression<Func<TDest, TMember>> destMember,
        Action<IMemberConfigurationExpression<TSource, TDest, TMember>> opts);

    IMappingExpression<TSource, TDest> ForPath<TMember>(
        Expression<Func<TDest, TMember>> destPath,
        Action<IMemberConfigurationExpression<TSource, TDest, TMember>> opts);
}
