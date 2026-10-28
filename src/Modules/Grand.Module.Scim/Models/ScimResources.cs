using Grand.Module.Scim.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grand.Module.Scim.Models;

public static class ScimSchemas
{
    public const string User = "urn:ietf:params:scim:schemas:core:2.0:User";
    public const string Group = "urn:ietf:params:scim:schemas:core:2.0:Group";
    public const string ListResponse = "urn:ietf:params:scim:api:messages:2.0:ListResponse";
    public const string Error = "urn:ietf:params:scim:api:messages:2.0:Error";
    public const string PatchOp = "urn:ietf:params:scim:api:messages:2.0:PatchOp";
    public const string ServiceProviderConfig = "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig";
    public const string ResourceType = "urn:ietf:params:scim:schemas:core:2.0:ResourceType";
    public const string Schema = "urn:ietf:params:scim:schemas:core:2.0:Schema";
}

public class ScimMeta
{
    public string ResourceType { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
    public string Location { get; set; }
}

public class ScimName
{
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string Formatted { get; set; }
}

/// <summary>A multi-valued attribute item: an e-mail, a group of a user, a member of a group</summary>
public class ScimMultiValue
{
    public string Value { get; set; }
    public string Type { get; set; }
    public bool? Primary { get; set; }
    public string Display { get; set; }

    [JsonPropertyName("$ref")]
    public string Ref { get; set; }
}

public class ScimUserResource
{
    public List<string> Schemas { get; set; }
    public string Id { get; set; }
    public string ExternalId { get; set; }
    public string UserName { get; set; }
    public ScimName Name { get; set; }
    public string DisplayName { get; set; }
    public List<ScimMultiValue> Emails { get; set; }

    [JsonConverter(typeof(ScimBooleanConverter))]
    public bool? Active { get; set; }

    public List<ScimMultiValue> Groups { get; set; }
    public ScimMeta Meta { get; set; }
}

public class ScimGroupResource
{
    public List<string> Schemas { get; set; }
    public string Id { get; set; }
    public string ExternalId { get; set; }
    public string DisplayName { get; set; }
    public List<ScimMultiValue> Members { get; set; }
    public ScimMeta Meta { get; set; }
}

public class ScimListResponse<T>
{
    public List<string> Schemas { get; set; } = [ScimSchemas.ListResponse];
    public int TotalResults { get; set; }
    public int StartIndex { get; set; }
    public int ItemsPerPage { get; set; }

    [JsonPropertyName("Resources")]
    public List<T> Resources { get; set; } = [];
}

public class ScimError
{
    public List<string> Schemas { get; set; } = [ScimSchemas.Error];

    /// <summary>The HTTP status as a string (RFC 7644, 3.12)</summary>
    public string Status { get; set; }

    public string ScimType { get; set; }
    public string Detail { get; set; }
}

public class ScimPatchRequest
{
    public List<string> Schemas { get; set; }
    public List<ScimPatchOperation> Operations { get; set; } = [];
}

public class ScimPatchOperation
{
    public string Op { get; set; }
    public string Path { get; set; }
    public JsonElement? Value { get; set; }
}
