using Grand.Module.Scim.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grand.Module.Scim.Protocol;

/// <summary>What a SCIM PATCH of a group changes; operations apply in order</summary>
public class ScimGroupChanges
{
    /// <summary>Members to add, or with <see cref="ReplaceAll" /> the whole new member list</summary>
    public List<string> Add { get; } = [];

    public List<string> Remove { get; } = [];
    public bool ReplaceAll { get; set; }
    public bool MembersChanged { get; set; }

    /// <summary>New name; null keeps it</summary>
    public string DisplayName { get; set; }

    /// <summary>New externalId; null keeps it</summary>
    public string ExternalId { get; set; }
}

/// <summary>
///     Reads a SCIM PATCH of a group in the shapes Entra ID and Okta send: members added and removed by value lists or by the
///     <c>members[value eq "id"]</c> path, a rename by path or by a value object without one
/// </summary>
public static partial class ScimGroupPatch
{
    [GeneratedRegex("""^members\[value eq "((?:[^"\\]|\\.)*)"\]$""", RegexOptions.IgnoreCase)]
    private static partial Regex MemberPath();

    public static bool TryApply(IList<ScimPatchOperation> operations, out ScimGroupChanges changes, out string error)
    {
        changes = new ScimGroupChanges();
        error = null;
        foreach (var operation in operations ?? [])
        {
            var op = operation.Op?.ToLowerInvariant();
            if (op is not ("add" or "replace" or "remove"))
            {
                error = "op must be add, replace or remove";
                return false;
            }

            if (string.IsNullOrEmpty(operation.Path))
            {
                if (op == "remove" || operation.Value is not { ValueKind: JsonValueKind.Object } value)
                {
                    error = "An operation without a path needs an object value";
                    return false;
                }

                foreach (var property in value.EnumerateObject())
                    switch (property.Name.ToLowerInvariant())
                    {
                        case "displayname":
                            changes.DisplayName = property.Value.ValueKind == JsonValueKind.String
                                ? property.Value.GetString()
                                : null;
                            break;
                        case "externalid":
                            changes.ExternalId = property.Value.ValueKind == JsonValueKind.String
                                ? property.Value.GetString()
                                : "";
                            break;
                        case "members":
                            ApplyMembers(changes, op, property.Value);
                            break;
                    }

                continue;
            }

            var memberPath = MemberPath().Match(operation.Path);
            if (memberPath.Success)
            {
                if (op != "remove")
                {
                    error = "Only remove is supported on a member filter path";
                    return false;
                }

                Remove(changes, [memberPath.Groups[1].Value.Replace("\\\"", "\"")]);
                continue;
            }

            switch (operation.Path.ToLowerInvariant())
            {
                case "members":
                    ApplyMembers(changes, op, operation.Value);
                    break;
                case "displayname":
                    if (op == "remove" || operation.Value is not { ValueKind: JsonValueKind.String } name)
                    {
                        error = "displayName must be a string";
                        return false;
                    }

                    changes.DisplayName = name.GetString();
                    break;
                case "externalid":
                    changes.ExternalId = op != "remove" && operation.Value is { ValueKind: JsonValueKind.String } id
                        ? id.GetString()
                        : "";
                    break;
                default:
                    error = "Unsupported path " + operation.Path;
                    return false;
            }
        }

        return true;
    }

    private static void ApplyMembers(ScimGroupChanges changes, string op, JsonElement? value)
    {
        var ids = MemberIds(value);
        switch (op)
        {
            case "add":
                changes.MembersChanged = true;
                foreach (var id in ids)
                {
                    changes.Remove.Remove(id);
                    if (!changes.Add.Contains(id))
                        changes.Add.Add(id);
                }

                break;
            case "replace":
                changes.MembersChanged = true;
                changes.ReplaceAll = true;
                changes.Add.Clear();
                changes.Remove.Clear();
                changes.Add.AddRange(ids.Distinct());
                break;
            default:
                //remove without a value empties the group
                if (value == null || value.Value.ValueKind == JsonValueKind.Null)
                {
                    changes.MembersChanged = true;
                    changes.ReplaceAll = true;
                    changes.Add.Clear();
                    changes.Remove.Clear();
                }
                else
                    Remove(changes, ids);

                break;
        }
    }

    private static void Remove(ScimGroupChanges changes, IEnumerable<string> ids)
    {
        changes.MembersChanged = true;
        foreach (var id in ids)
        {
            changes.Add.Remove(id);
            if (!changes.Remove.Contains(id))
                changes.Remove.Add(id);
        }
    }

    //[{"value":"id"}], or one {"value":"id"}
    private static List<string> MemberIds(JsonElement? value)
    {
        if (value == null)
            return [];
        var items = value.Value.ValueKind switch {
            JsonValueKind.Array => value.Value.EnumerateArray().ToList(),
            JsonValueKind.Object => [value.Value],
            _ => []
        };
        return items
            .Where(x => x.ValueKind == JsonValueKind.Object && x.TryGetProperty("value", out var v) &&
                        v.ValueKind == JsonValueKind.String)
            .Select(x => x.GetProperty("value").GetString())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();
    }
}
