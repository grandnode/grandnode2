using System.Linq.Expressions;

namespace Grand.Data.LiteDb;

public class ToLowerInvariantVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method == typeof(string).GetMethod("ToLowerInvariant", Type.EmptyTypes))
        {
            var invariantMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            if (invariantMethod != null) return Expression.Call(node.Object, invariantMethod);
        }

        return base.VisitMethodCall(node);
    }
}