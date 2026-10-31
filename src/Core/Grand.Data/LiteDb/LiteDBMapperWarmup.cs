using LiteDB;
using System.Reflection;

namespace Grand.Data.LiteDb;

/// <summary>
///     Builds the entity mappers before the application starts handling requests and running tasks.
///     LiteDB publishes a mapper before filling its members and reads published mappers without a lock
///     (BsonMapper.BuildAddEntityMapper), so threads meeting a type for the first time together either
///     throw "Collection was modified" or silently read an object with fields missing.
/// </summary>
public static class LiteDBMapperWarmup
{
    private static readonly string[] FrameworkNamespaces = ["System", "Microsoft", "LiteDB", "MongoDB"];

    //Entity<T> is the public way to reach the internal BsonMapper.GetEntityMapper
    private static readonly MethodInfo BuildMapper = typeof(BsonMapper).GetMethod(nameof(BsonMapper.Entity))!;

    public static void Warm(BsonMapper mapper, IEnumerable<Type> entityTypes)
    {
        var visited = new HashSet<Type>();
        var pending = new Stack<Type>(entityTypes);

        while (pending.TryPop(out var type))
        {
            if (!visited.Add(type)) continue;

            if (type.IsArray)
            {
                pending.Push(type.GetElementType()!);
                continue;
            }

            //lists, dictionaries and nullables hold their mapped types in the type arguments
            foreach (var argument in type.GetGenericArguments()) pending.Push(argument);

            if (type.IsEnum || type.ContainsGenericParameters || IsFrameworkType(type)) continue;

            BuildMapper.MakeGenericMethod(type).Invoke(mapper, null);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                pending.Push(property.PropertyType);

            //LINQ looks a member up on the type declaring it: x.Id goes through ParentEntity, not the entity
            if (type.BaseType != null) pending.Push(type.BaseType);
            foreach (var contract in type.GetInterfaces()) pending.Push(contract);
        }
    }

    private static bool IsFrameworkType(Type type)
    {
        return type.Namespace != null &&
               FrameworkNamespaces.Any(name => type.Namespace == name || type.Namespace.StartsWith(name + "."));
    }
}
