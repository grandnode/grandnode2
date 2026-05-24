using System.Linq.Expressions;

namespace Grand.Mapping.Internal;

/// <summary>
/// Substitutes a lambda parameter with a concrete expression, enabling body inlining
/// instead of generating a delegate-call wrapper via Expression.Invoke.
/// </summary>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;
    private readonly Expression _replacement;

    internal ParameterReplacer(ParameterExpression parameter, Expression replacement)
    {
        _parameter = parameter;
        _replacement = replacement;
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _parameter ? _replacement : base.VisitParameter(node);
}
