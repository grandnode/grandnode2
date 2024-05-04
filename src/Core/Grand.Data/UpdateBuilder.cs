using MongoDB.Driver;
using System.Linq.Expressions;

namespace Grand.Data;

public class UpdateBuilder<T>
{
    private readonly List<ExpressionFieldDefinition<T, object>> _expressionFieldDefinitions = new();
    private readonly List<UpdateDefinition<T>> _list = new();

    protected UpdateBuilder() { }

    public IEnumerable<UpdateDefinition<T>> Fields => _list;

    public IEnumerable<ExpressionFieldDefinition<T, object>> ExpressionFields => _expressionFieldDefinitions;

    public static UpdateBuilder<T> Create()
    {
        return new UpdateBuilder<T>();
    }

    public UpdateBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> selector, TProperty value)
    {
        //for mongodb
        _list.Add(Builders<T>.Update.Set(selector, value));

        //for other Db
        _expressionFieldDefinitions.Add(new ExpressionFieldDefinition<T, object>(selector, value));

        return this;
    }
}