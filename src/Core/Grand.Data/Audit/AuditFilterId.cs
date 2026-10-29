using Grand.Domain;
using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Data.Audit;

/// <summary>
///     Reads what a repository write touches from its expressions: the document id of a filter, and the field paths.
/// </summary>
public static class AuditFilterId
{
    /// <summary>
    ///     Recognises <c>x => x.Id == value</c> (either operand order) and evaluates the value
    /// </summary>
    public static bool TryGetId<T>(Expression<Func<T, bool>> filter, out string id) where T : BaseEntity
    {
        id = null;
        if (filter?.Body is not BinaryExpression { NodeType: ExpressionType.Equal } equal)
            return false;

        var parameter = filter.Parameters[0];
        if (IsIdOf(equal.Left, parameter))
            return TryEvaluate(equal.Right, out id);
        if (IsIdOf(equal.Right, parameter))
            return TryEvaluate(equal.Left, out id);
        return false;
    }

    /// <summary>
    ///     The dotted member path of <c>x => x.A.B</c>, or null when the expression is not a plain member chain
    /// </summary>
    public static string MemberPath<T, TField>(Expression<Func<T, TField>> expression)
    {
        return MemberPath((LambdaExpression)expression);
    }

    public static string MemberPath(LambdaExpression expression)
    {
        var names = new Stack<string>();
        var current = StripConvert(expression.Body);
        while (current is MemberExpression member)
        {
            names.Push(member.Member.Name);
            current = StripConvert(member.Expression);
        }

        return current == expression.Parameters[0] && names.Count > 0 ? string.Join(".", names) : null;
    }

    /// <summary>
    ///     The member paths an update builder sets
    /// </summary>
    public static IList<string> MemberPaths<T>(UpdateBuilder<T> builder)
    {
        return builder.ExpressionFields.Select(f => MemberPath(f.Expression)).ToList();
    }

    private static bool IsIdOf(Expression expression, ParameterExpression parameter)
    {
        return StripConvert(expression) is MemberExpression { Member.Name: nameof(BaseEntity.Id) } member
               && StripConvert(member.Expression) == parameter;
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            expression = unary.Operand;
        return expression;
    }

    private static bool TryEvaluate(Expression expression, out string value)
    {
        value = null;
        if (!TryEvaluateObject(StripConvert(expression), out var result))
            return false;

        value = result?.ToString();
        return !string.IsNullOrEmpty(value);
    }

    //constants and captured variables only, without compiling the expression
    private static bool TryEvaluateObject(Expression expression, out object value)
    {
        value = null;
        switch (expression)
        {
            case ConstantExpression constant:
                value = constant.Value;
                return true;
            case MemberExpression member:
                object target = null;
                if (member.Expression != null && !TryEvaluateObject(StripConvert(member.Expression), out target))
                    return false;
                if (member.Expression != null && target == null)
                    return false;
                try
                {
                    value = member.Member switch {
                        FieldInfo field => field.GetValue(target),
                        PropertyInfo property => property.GetValue(target),
                        _ => null
                    };
                }
                catch (Exception)
                {
                    //a getter that throws: the write is recorded as a bulk write instead of failing
                    return false;
                }

                return member.Member is FieldInfo or PropertyInfo;
            default:
                return false;
        }
    }
}
