using Grand.Module.Scim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace Grand.Module.Scim.Infrastructure;

/// <summary>Responses of the SCIM endpoint, in application/scim+json</summary>
public static class ScimResponse
{
    public static ContentResult Json(object body, int status = 200) =>
        new() {
            StatusCode = status,
            ContentType = ScimJson.ContentType + "; charset=utf-8",
            Content = JsonSerializer.Serialize(body, body.GetType(), ScimJson.Options)
        };

    /// <param name="scimType">RFC 7644 error type (invalidFilter, uniqueness, invalidValue...), null for none</param>
    public static ContentResult Error(int status, string scimType, string detail) =>
        Json(new ScimError {
            Status = status.ToString(CultureInfo.InvariantCulture),
            ScimType = scimType,
            Detail = detail
        }, status);
}
