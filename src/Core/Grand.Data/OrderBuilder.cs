using System.Linq.Expressions;

namespace Grand.Data;

public class OrderBuilder<T>
{
    private readonly List<(Expression<Func<T, object>> selector, bool value, string fieldName)> _list = new();

    protected OrderBuilder() { }

    public IEnumerable<(Expression<Func<T, object>> selector, bool value, string fieldName)> Fields => _list;

    public static OrderBuilder<T> Create()
    {
        return new OrderBuilder<T>();
    }

    public OrderBuilder<T> Ascending(Expression<Func<T, object>> selector)
    {
        _list.Add((selector, true, ""));

        return this;
    }

    public OrderBuilder<T> Ascending(string fieldName)
    {
        _list.Add((null, true, fieldName));

        return this;
    }

    public OrderBuilder<T> Descending(Expression<Func<T, object>> selector)
    {
        _list.Add((selector, false, ""));

        return this;
    }

    public OrderBuilder<T> Descending(string fieldName)
    {
        _list.Add((null, false, fieldName));

        return this;
    }
}