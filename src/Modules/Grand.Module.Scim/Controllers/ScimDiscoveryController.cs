using Grand.Infrastructure.Configuration;
using Grand.Module.Scim.Infrastructure;
using Grand.Module.Scim.Models;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Module.Scim.Controllers;

/// <summary>SCIM discovery (RFC 7644, section 4): what this service provider supports</summary>
public class ScimDiscoveryController(ScimConfig config) : ScimControllerBase
{
    [HttpGet("ServiceProviderConfig")]
    public IActionResult ServiceProviderConfig() =>
        ScimResponse.Json(new {
            schemas = new[] { ScimSchemas.ServiceProviderConfig },
            patch = new { supported = true },
            bulk = new { supported = false, maxOperations = 0, maxPayloadSize = 0 },
            filter = new { supported = true, maxResults = config.PageSize(null) },
            changePassword = new { supported = false },
            sort = new { supported = false },
            etag = new { supported = false },
            authenticationSchemes = new[] {
                new {
                    type = "oauthbearertoken",
                    name = "Bearer token",
                    description = "The long-lived token of Scim:Token in appsettings",
                    primary = true
                }
            },
            meta = new { resourceType = "ServiceProviderConfig", location = $"{BaseUrl}/ServiceProviderConfig" }
        });

    [HttpGet("ResourceTypes")]
    public IActionResult ResourceTypes() =>
        ScimResponse.Json(new ScimListResponse<object> {
            TotalResults = 2,
            StartIndex = 1,
            ItemsPerPage = 2,
            Resources = [ResourceType("User", "/Users", ScimSchemas.User), ResourceType("Group", "/Groups", ScimSchemas.Group)]
        });

    [HttpGet("Schemas")]
    public IActionResult Schemas() =>
        ScimResponse.Json(new ScimListResponse<object> {
            TotalResults = 2,
            StartIndex = 1,
            ItemsPerPage = 2,
            Resources = [
                Schema(ScimSchemas.User, "User", [
                    Attribute("userName", "string", true, true),
                    Attribute("externalId", "string", false, true),
                    Attribute("name", "complex", false, false),
                    Attribute("emails", "complex", false, false, true),
                    Attribute("active", "boolean", false, false),
                    Attribute("groups", "complex", false, false, true, "readOnly")
                ]),
                Schema(ScimSchemas.Group, "Group", [
                    Attribute("displayName", "string", true, false),
                    Attribute("externalId", "string", false, true),
                    Attribute("members", "complex", false, false, true)
                ])
            ]
        });

    private object ResourceType(string name, string endpoint, string schema) =>
        new {
            schemas = new[] { ScimSchemas.ResourceType },
            id = name,
            name,
            endpoint,
            schema,
            meta = new { resourceType = "ResourceType", location = $"{BaseUrl}/ResourceTypes/{name}" }
        };

    private object Schema(string id, string name, object[] attributes) =>
        new {
            schemas = new[] { ScimSchemas.Schema },
            id,
            name,
            attributes,
            meta = new { resourceType = "Schema", location = $"{BaseUrl}/Schemas/{id}" }
        };

    private static object Attribute(string name, string type, bool required, bool caseExact, bool multiValued = false,
        string mutability = "readWrite") =>
        new { name, type, multiValued, required, caseExact, mutability, returned = "default", uniqueness = "none" };
}
