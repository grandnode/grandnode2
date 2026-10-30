using Grand.Data.Audit;
using Grand.Domain.Audit;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Puts names next to the ids of [AuditReference] properties and the numbers of enums when a change is recorded,
///     so the entry stays readable after the referenced record is gone
/// </summary>
public partial class AuditReferences(IAuditRecordReader reader)
{
    private static readonly ConcurrentDictionary<Type, string> NameProperties = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> Properties = new();

    private readonly Dictionary<(Type Target, string List, string Key, string Owner, string Id), string> _names = new();

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex ElementKeys();

    /// <summary>
    ///     The property that names a record of a type: the one marked [AuditName], else Name
    /// </summary>
    public static string NameProperty(Type type)
    {
        return NameProperties.GetOrAdd(type, t =>
            t.GetProperties().FirstOrDefault(p => p.IsDefined(typeof(AuditNameAttribute)))?.Name ?? "Name");
    }

    public async Task Describe(Type entityType, IEnumerable<AuditChange> changes, IDictionary<string, string> before,
        IDictionary<string, string> after)
    {
        //a cut value is no longer a whole id list; it stays as stored
        foreach (var change in changes.Where(c => !c.Masked && !c.Truncated))
        {
            var property = PropertyAt(entityType, change.Path);
            if (property == null)
                continue;

            //a name that cannot be found leaves the ids, never the change out
            try
            {
                var described = AuditDiff.Change(change.Path,
                    await Describe(property, change.Path, change.OldValue, before),
                    await Describe(property, change.Path, change.NewValue, after));
                change.OldValue = described.OldValue;
                change.NewValue = described.NewValue;
                change.Truncated = described.Truncated;
            }
            catch (Exception)
            {
                //keep the stored values
            }
        }
    }

    /// <summary>
    ///     Names the list elements each changed path runs through (see <see cref="AuditChange.ElementNames" />), so a
    ///     change reads "Color family" instead of an element id
    /// </summary>
    public async Task NameElements(Type entityType, IEnumerable<AuditChange> changes,
        IDictionary<string, string> before, IDictionary<string, string> after)
    {
        var named = new Dictionary<string, string>();
        foreach (var change in changes)
        {
            //a name that cannot be found leaves the element ids, never the change out
            try
            {
                var names = new List<string>();
                var type = entityType;
                var path = "";
                foreach (var segment in change.Path.Split('.'))
                {
                    path = path == "" ? segment : $"{path}.{segment}";
                    var property = type?.GetProperty(segment.Split('[')[0]);
                    if (property == null)
                        break;

                    var elementType = ElementType(property.PropertyType);
                    if (elementType != null && segment.Contains("[id="))
                    {
                        if (!named.TryGetValue(path, out var name))
                            named[path] = name = await ElementName(elementType, path, before, after);
                        names.Add(name ?? "");
                    }

                    type = elementType ?? property.PropertyType;
                }

                if (names.Any(n => n != ""))
                    change.ElementNames = names;
            }
            catch (Exception)
            {
                //keep the path only
            }
        }
    }

    //the element's [AuditName] or Name value; a reference names it by the referenced record
    private async Task<string> ElementName(Type elementType, string path, IDictionary<string, string> before,
        IDictionary<string, string> after)
    {
        var property = elementType.GetProperty(NameProperty(elementType));
        if (property == null)
            return null;

        var field = $"{path}.{property.Name}";
        var snapshot = after != null && after.ContainsKey(field) ? after : before;
        if (snapshot == null || !snapshot.TryGetValue(field, out var value) || string.IsNullOrEmpty(value))
            return null;

        var reference = property.GetCustomAttribute<AuditReferenceAttribute>();
        if (reference == null || reference.Target.IsEnum)
            return value;

        var owner = reference.Owner == null ? null : SiblingValue(snapshot, field, reference.Owner);
        return await NameOrNull(reference, owner, value);
    }

    /// <summary>
    ///     Whether the values of a path are stored as "name (id)": a reference, or a number of an enum
    /// </summary>
    public static bool HasNamedValues(Type entityType, string path)
    {
        var property = PropertyAt(entityType, path);
        return property != null
               && (property.IsDefined(typeof(AuditReferenceAttribute))
                   || (Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType).IsEnum);
    }

    /// <summary>
    ///     The property a changed path ends at, e.g. "ProductSpecificationAttributes[id=1].SpecificationAttributeOptionId"
    ///     is SpecificationAttributeOptionId of the element type; null when the path is not a property
    /// </summary>
    public static PropertyInfo PropertyAt(Type type, string path)
    {
        //cached by the path without its element keys, so the cache grows with the model, not with the data
        return Properties.GetOrAdd((type, ElementKeys().Replace(path, "")), key => FindProperty(key.Item1, key.Item2));
    }

    private static PropertyInfo FindProperty(Type type, string path)
    {
        PropertyInfo property = null;
        foreach (var segment in path.Split('.'))
        {
            property = type?.GetProperty(segment.Split('[')[0]);
            if (property == null)
                return null;

            type = ElementType(property.PropertyType) ?? property.PropertyType;
        }

        return property;
    }

    private static Type ElementType(Type type)
    {
        return type == typeof(string)
            ? null
            : type.GetInterfaces().Append(type)
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
    }

    private async Task<string> Describe(PropertyInfo property, string path, string value,
        IDictionary<string, string> snapshot)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var reference = property.GetCustomAttribute<AuditReferenceAttribute>();
        var enumType = reference?.Target is { IsEnum: true } target
            ? target
            : Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (enumType.IsEnum)
            return long.TryParse(value, out var number) && Enum.GetName(enumType, Enum.ToObject(enumType, number)) is { } name
                ? $"{name} ({value})"
                : value;

        if (reference == null)
            return value;

        var owner = reference.Owner == null ? null : SiblingValue(snapshot, path, reference.Owner);
        if (ElementType(property.PropertyType) == null)
            return await Label(reference, owner, value);

        //a list of ids is stored as one JSON array value
        var described = new List<string>();
        foreach (var id in JsonSerializer.Deserialize<string[]>(value) ?? [])
            described.Add(await Label(reference, owner, id));
        return string.Join(", ", described);
    }

    private static string SiblingValue(IDictionary<string, string> snapshot, string path, string sibling)
    {
        var parent = path.Contains('.') ? path[..(path.LastIndexOf('.') + 1)] : "";
        return snapshot != null && snapshot.TryGetValue(parent + sibling, out var value) ? value : null;
    }

    private async Task<string> Label(AuditReferenceAttribute reference, string owner, string id)
    {
        var name = await NameOrNull(reference, owner, id);
        return name == null ? id : $"{name} ({id})";
    }

    private async Task<string> NameOrNull(AuditReferenceAttribute reference, string owner, string id)
    {
        var key = (reference.Target, reference.List, reference.Key, owner, id);
        if (!_names.TryGetValue(key, out var name))
        {
            name = await NameOf(reference, owner, id);
            _names[key] = name;
        }

        return string.IsNullOrEmpty(name) ? null : name;
    }

    //the name in the snapshot of the referenced record, or of the element of its list
    private async Task<string> NameOf(AuditReferenceAttribute reference, string owner, string id)
    {
        var recordId = reference.List == null ? id : owner;
        if (string.IsNullOrEmpty(recordId))
            return null;

        //only the field holding the name is read, not the whole record
        var record = reference.List == null
            ? await reader.Read(reference.Target, recordId, reference.Key, NameProperty(reference.Target))
            : await reader.Read(reference.Target, recordId, null, reference.List);
        var path = reference.List == null
            ? NameProperty(reference.Target)
            : $"{reference.List}[id={id}].{NameProperty(ElementType(reference.Target.GetProperty(reference.List)!.PropertyType)!)}";
        return record != null && record.TryGetValue(path, out var name) && name != "" ? name : null;
    }
}
