using Grand.Business.Core.Utilities.Authentication;
using Grand.Module.Scim.Infrastructure;
using Grand.Module.Scim.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grand.Module.Scim.Protocol;

/// <summary>
///     Applies a SCIM PATCH to the kept attributes of a user. Accepts the shapes Entra ID and Okta send: a path or a value
///     object without one, ops in any letter case, booleans as strings. Attributes GrandNode does not keep (title, department,
///     phone numbers...) are ignored, so a provider's default attribute mapping does not fail
/// </summary>
public static partial class ScimUserPatch
{
    private const string UserSchemaPrefix = ScimSchemas.User + ":";

    [GeneratedRegex("""^emails\[type eq "[a-z]+"\]\.value$""", RegexOptions.IgnoreCase)]
    private static partial Regex TypedEmailPath();

    public static bool TryApply(ScimUserData user, IList<ScimPatchOperation> operations, out ScimUserData result,
        out string error)
    {
        result = user;
        error = null;
        foreach (var operation in operations ?? [])
        {
            var op = operation.Op?.ToLowerInvariant();
            if (op is not ("add" or "replace" or "remove"))
            {
                error = "op must be add, replace or remove";
                return false;
            }

            if (!string.IsNullOrEmpty(operation.Path))
            {
                if (!TryApplyPath(ref result, operation.Path, op == "remove" ? null : operation.Value, out error))
                    return false;
                continue;
            }

            if (op == "remove" || operation.Value is not { ValueKind: JsonValueKind.Object } value)
            {
                error = "An operation without a path needs an object value";
                return false;
            }

            foreach (var property in value.EnumerateObject())
                if (!TryApplyPath(ref result, property.Name, property.Value, out error))
                    return false;
        }

        return true;
    }

    /// <param name="value">Null for a remove</param>
    private static bool TryApplyPath(ref ScimUserData user, string path, JsonElement? value, out string error)
    {
        error = null;
        if (path.StartsWith(UserSchemaPrefix, StringComparison.OrdinalIgnoreCase))
            path = path[UserSchemaPrefix.Length..];

        switch (path.ToLowerInvariant())
        {
            case "active":
                if (value == null)
                    return true;
                if (!ScimJson.TryReadBoolean(value.Value, out var active))
                {
                    error = "active must be a boolean";
                    return false;
                }

                user = user with { Active = active };
                return true;
            case "username":
                return TryString(value, "userName", out var userName, out error) &&
                       Set(ref user, user with { UserName = userName });
            case "externalid":
                return TryString(value, "externalId", out var externalId, out error) &&
                       Set(ref user, user with { ExternalId = externalId });
            case "name.givenname":
                return TryString(value, "name.givenName", out var givenName, out error) &&
                       Set(ref user, user with { GivenName = givenName });
            case "name.familyname":
                return TryString(value, "name.familyName", out var familyName, out error) &&
                       Set(ref user, user with { FamilyName = familyName });
            case "name":
                if (value is not { ValueKind: JsonValueKind.Object } name)
                    return Set(ref user, user with { GivenName = null, FamilyName = null });
                foreach (var part in name.EnumerateObject())
                    if (part.Name.ToLowerInvariant() is "givenname" or "familyname" &&
                        !TryApplyPath(ref user, "name." + part.Name, part.Value, out error))
                        return false;
                return true;
            case "emails":
                return Set(ref user, user with { Email = PickEmail(value) });
            default:
                if (TypedEmailPath().IsMatch(path))
                    return TryString(value, "emails.value", out var email, out error) &&
                           Set(ref user, user with { Email = email });
                return true;
        }
    }

    //the primary e-mail, else the work e-mail, else the first one
    private static string PickEmail(JsonElement? value)
    {
        if (value is not { ValueKind: JsonValueKind.Array } emails)
            return null;
        var items = emails.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).ToList();
        var chosen = items.FirstOrDefault(x => x.TryGetProperty("primary", out var p) && ScimJson.TryReadBoolean(p, out var b) && b);
        if (chosen.ValueKind == JsonValueKind.Undefined)
            chosen = items.FirstOrDefault(x =>
                x.TryGetProperty("type", out var t) && string.Equals(t.GetString(), "work", StringComparison.OrdinalIgnoreCase));
        if (chosen.ValueKind == JsonValueKind.Undefined)
            chosen = items.FirstOrDefault();
        return chosen.ValueKind == JsonValueKind.Object && chosen.TryGetProperty("value", out var v) &&
               v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
    }

    private static bool TryString(JsonElement? value, string name, out string result, out string error)
    {
        error = null;
        result = null;
        if (value == null || value.Value.ValueKind == JsonValueKind.Null)
            return true;
        if (value.Value.ValueKind == JsonValueKind.String)
        {
            result = value.Value.GetString();
            return true;
        }

        error = name + " must be a string";
        return false;
    }

    private static bool Set(ref ScimUserData user, ScimUserData changed)
    {
        user = changed;
        return true;
    }
}
