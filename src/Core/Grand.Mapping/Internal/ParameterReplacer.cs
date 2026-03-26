using System.Linq.Expressions;

namespace Grand.Mapping.Internal;

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly Expression _newExpr;

    private ParameterReplacer(ParameterExpression oldParam, Expression newExpr)
    {
        _oldParam = oldParam;
        _newExpr = newExpr;
    }

    public static Expression Replace(Expression body, ParameterExpression oldParam, Expression newExpr)
        => new ParameterReplacer(oldParam, newExpr).Visit(body)!;

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _oldParam ? _newExpr : base.VisitParameter(node);
}
