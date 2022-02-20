using System.Linq.Expressions;

namespace Grand.Domain.Data
{
    public class ExpressionFieldDefinition<TDocument, TField>
    {
        public ExpressionFieldDefinition(LambdaExpression expression, TField value)
        {
            Expression = expression;
            Value = value;
        }

        public LambdaExpression Expression { get; }
        public TField Value { get; }

    }
}
