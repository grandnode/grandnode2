using System.Linq.Expressions;

namespace Grand.Mapping;

public interface IMemberConfigurationExpression<TSource, TDest, TMember>
{
    void Ignore();
    void MapFrom<TResult>(Expression<Func<TSource, TResult>> mapExpression);
    void Condition(Expression<Func<TSource, bool>> condition);
}
