using System.Linq.Expressions;

namespace Grand.Mapping.Internal;

internal sealed class MappingExpressionImpl<TSource, TDest> : IMappingExpression<TSource, TDest>
{
    internal readonly List<MemberConfig> MemberConfigs = new();

    public IMappingExpression<TSource, TDest> ForMember<TMember>(
        Expression<Func<TDest, TMember>> destMember,
        Action<IMemberConfigurationExpression<TSource, TDest, TMember>> opts)
    {
        var body = destMember.Body;
        // Unwrap UnaryExpression conversion if present (e.g., boxing)
        if (body is UnaryExpression unary) body = unary.Operand;
        var config = new MemberConfig {
            MemberName = ((MemberExpression)body).Member.Name,
            IsPath = false
        };
        opts(new MemberConfigExpressionImpl<TSource, TDest, TMember>(config));
        MemberConfigs.Add(config);
        return this;
    }

    public IMappingExpression<TSource, TDest> ForPath<TMember>(
        Expression<Func<TDest, TMember>> destPath,
        Action<IMemberConfigurationExpression<TSource, TDest, TMember>> opts)
    {
        var config = new MemberConfig {
            DestinationPathExpression = destPath,
            IsPath = true
        };
        opts(new MemberConfigExpressionImpl<TSource, TDest, TMember>(config));
        MemberConfigs.Add(config);
        return this;
    }
}

internal sealed class MemberConfigExpressionImpl<TSource, TDest, TMember>
    : IMemberConfigurationExpression<TSource, TDest, TMember>
{
    private readonly MemberConfig _config;

    public MemberConfigExpressionImpl(MemberConfig config) => _config = config;

    public void Ignore() => _config.IsIgnored = true;

    public void MapFrom<TResult>(Expression<Func<TSource, TResult>> mapExpression)
        => _config.MapFromExpression = mapExpression;

    public void Condition(Expression<Func<TSource, bool>> condition)
        => _config.ConditionExpression = condition;
}
