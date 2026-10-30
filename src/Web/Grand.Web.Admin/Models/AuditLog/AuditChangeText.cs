using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Infrastructure.Audit;
using System.Text.RegularExpressions;

namespace Grand.Web.Admin.Models.AuditLog;

/// <summary>
///     Puts the changes of an entry in words for a person: the changes of one list element as one line named by the
///     element, field names instead of property paths, Yes/No, and names without the ids stored next to them.
///     The stored paths and values stay as they are for the entry detail and the CSV export.
/// </summary>
public partial class AuditChangeText(Func<string, string> resource)
{
    public const int ValueLength = 60;
    public const int TextLength = 300;
    public const int SummaryLength = 200;

    private static readonly Lazy<Dictionary<string, Type>> EntityTypes = new(() =>
        typeof(BaseEntity).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(BaseEntity).IsAssignableFrom(t))
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.First()));

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordBreaks();

    [GeneratedRegex(@"Id\d*$")]
    private static partial Regex IdSuffix();

    //the " (id)" stored after a name, in one value or in a list of them
    [GeneratedRegex(@" \([^()]*\)(?=, |$)")]
    private static partial Regex StoredIds();

    /// <summary>
    ///     Every change with its values, one line per list element, e.g.
    ///     "Specification attributes › Color family: added (Specification attribute option: Black, Display order: 0)"
    /// </summary>
    public string Text(string entityType, IEnumerable<AuditChange> changes)
    {
        var type = EntityTypes.Value.GetValueOrDefault(entityType ?? "");
        return Cut(string.Join(" · ", Lines(entityType, changes).Select(line => LineText(type, line))), TextLength);
    }

    /// <summary>
    ///     The changes of an entry; an entry of a record that belongs to another (a note in its order's history) says
    ///     what it is, e.g. "Order note: Note: — → Called the customer", or "Order note Called the customer" when deleted
    /// </summary>
    public string Text(AuditEntry entry)
    {
        var text = Text(entry.EntityType, entry.Changes);
        //an entry without changes (a bulk write, an event) tells what happened in its details
        if (text == "" && !string.IsNullOrEmpty(entry.Details))
            text = Cut(entry.Details, TextLength);
        if (entry.OwnerType == null)
            return text;

        var what = Word(entry.EntityType, null);
        if (text == "")
            return string.IsNullOrEmpty(entry.EntityName) ? what : $"{what} {Cut(entry.EntityName, ValueLength)}";
        return Cut($"{what}: {text}", TextLength);
    }

    /// <summary>
    ///     What changed without the values, for the audit log list
    /// </summary>
    public string Summary(string entityType, IEnumerable<AuditChange> changes)
    {
        return Cut(string.Join(" · ", Lines(entityType, changes).Select(line => line.Kind switch {
            LineKind.Added => $"{line.Label} ({resource("Admin.System.AuditLog.Added")})",
            LineKind.Removed => $"{line.Label} ({resource("Admin.System.AuditLog.Removed")})",
            _ when line.Element => $"{line.Label}: {string.Join(", ", line.Changes.Select(c => c.Field))}",
            _ => line.Label
        })), SummaryLength);
    }

    /// <summary>
    ///     The field name of one change, e.g. "Specification attributes › Color family › Display order"
    /// </summary>
    public string Label(string entityType, AuditChange change)
    {
        var line = Lines(entityType, [change]).Single();
        return line.Element ? $"{line.Label} › {line.Changes[0].Field}" : line.Label;
    }

    private enum LineKind
    {
        Changed,
        Added,
        Removed
    }

    private sealed record FieldChange(string Field, AuditChange Change);

    private sealed record Line(string Label, string ElementName, bool Element, List<FieldChange> Changes)
    {
        public LineKind Kind =>
            !Element ? LineKind.Changed
            : Changes.All(c => !c.Change.Masked && c.Change.OldValue == null) ? LineKind.Added
            : Changes.All(c => !c.Change.Masked && c.Change.NewValue == null) ? LineKind.Removed
            : LineKind.Changed;
    }

    //the changes of one list element (the path up to its last [key]) go to one line, in the order they came
    private static List<Line> Lines(string entityType, IEnumerable<AuditChange> changes)
    {
        var lines = new List<Line>();
        var byElement = new Dictionary<string, Line>();
        foreach (var change in changes)
        {
            var segments = Segments(change.Path);
            var last = segments.FindLastIndex(s => s.Key != null);
            if (last < 0)
            {
                lines.Add(new Line(Words(segments, entityType), null, false, [new FieldChange(null, change)]));
                continue;
            }

            var field = Words(segments.Skip(last + 1), entityType);
            var element = string.Join(".", segments.Take(last + 1).Select(s => s.Text));
            if (!byElement.TryGetValue(element, out var line))
            {
                var (label, name) = ElementLabel(segments.Take(last + 1).ToList(), change.ElementNames, entityType);
                byElement[element] = line = new Line(label, name, true, []);
                lines.Add(line);
            }

            line.Changes.Add(new FieldChange(field, change));
        }

        return lines;
    }

    //"ProductSpecificationAttributes[id=…]" -> "Specification attributes › Color family"; an element without a name
    //stays unnamed rather than shown by its id
    private static (string Label, string Name) ElementLabel(List<Segment> segments, IList<string> names,
        string entityType)
    {
        var parts = new List<string>();
        string name = null;
        var named = 0;
        foreach (var segment in segments)
        {
            parts.Add(Word(segment.Name, entityType));
            if (segment.Key == null)
                continue;

            name = segment.Key.StartsWith("id=")
                ? names?.ElementAtOrDefault(named++) is { Length: > 0 } elementName ? elementName : null
                : int.TryParse(segment.Key, out var index)
                    ? $"#{index + 1}"
                    : segment.Key;
            if (name != null)
                parts.Add(name);
        }

        return (string.Join(" › ", parts), name);
    }

    private string LineText(Type type, Line line)
    {
        string Value(FieldChange field, string value) => ValueText(type, field.Change, value);

        switch (line.Kind)
        {
            case LineKind.Added:
                //the field that names the element is already in the label
                var set = line.Changes
                    .Select(c => (c.Field, Value: Value(c, c.Change.NewValue)))
                    .Where(c => c.Value != line.ElementName)
                    .Select(c => $"{c.Field}: {c.Value}")
                    .ToList();
                var added = $"{line.Label}: {resource("Admin.System.AuditLog.Added")}";
                return set.Count == 0 ? added : $"{added} ({string.Join(", ", set)})";
            case LineKind.Removed:
                return $"{line.Label}: {resource("Admin.System.AuditLog.Removed")}";
            default:
                var fields = line.Changes.Select(c => c.Change.Masked
                    ? $"{c.Field ?? line.Label}: {resource("Admin.System.AuditLog.ChangedHidden")}"
                    : $"{c.Field ?? line.Label}: {Value(c, c.Change.OldValue)} → {Value(c, c.Change.NewValue)}");
                return line.Element ? $"{line.Label}: {string.Join(", ", fields)}" : string.Join(", ", fields);
        }
    }

    private string ValueText(Type type, AuditChange change, string value)
    {
        if (value == null)
            return "—";
        if (value is "true" or "false")
            return resource(value == "true" ? "Admin.Common.Yes" : "Admin.Common.No");
        if (type != null && AuditReferences.HasNamedValues(type, change.Path))
            value = StoredIds().Replace(value, "");
        //a value cut when it was stored ends in "…" too
        var text = Cut(value, ValueLength);
        return change.Truncated && !text.EndsWith('…') ? text + "…" : text;
    }

    private static string Cut(string value, int length)
    {
        return value.Length > length ? value[..length].TrimEnd() + "…" : value;
    }

    private sealed record Segment(string Text, string Name, string Key);

    //split on the dots outside [ ], so a key with a dot stays whole
    private static List<Segment> Segments(string path)
    {
        var segments = new List<Segment>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i <= path.Length; i++)
        {
            if (i < path.Length)
            {
                if (path[i] == '[') depth++;
                else if (path[i] == ']') depth--;
                if (path[i] != '.' || depth > 0)
                    continue;
            }

            var text = path[start..i];
            var bracket = text.IndexOf('[');
            segments.Add(bracket < 0
                ? new Segment(text, text, null)
                : new Segment(text, text[..bracket], text[(bracket + 1)..].TrimEnd(']')));
            start = i + 1;
        }

        return segments;
    }

    private static string Words(IEnumerable<Segment> segments, string entityType)
    {
        return string.Join(" › ", segments.Select(s => Word(s.Name, entityType)));
    }

    //"ProductSpecificationAttributes" of a Product -> "Specification attributes", "SpecificationAttributeOptionId" ->
    //"Specification attribute option"
    private static string Word(string name, string entityType)
    {
        var word = IdSuffix().Replace(name, "") is { Length: > 0 } withoutId ? withoutId : name;
        if (!string.IsNullOrEmpty(entityType) && word.Length > entityType.Length && word.StartsWith(entityType)
            && char.IsUpper(word[entityType.Length]))
            word = word[entityType.Length..];

        var words = WordBreaks().Split(word).Where(w => w != "").ToList();
        return string.Join(" ", words.Select((w, i) =>
            i == 0 ? char.ToUpperInvariant(w[0]) + w[1..]
            : w.All(char.IsUpper) ? w
            : w.ToLowerInvariant()));
    }
}
