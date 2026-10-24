using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Grand.Module.Scim.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Grand.Module.Scim.Controllers;

/// <summary>
///     Base of the SCIM 2.0 endpoint: the identity provider's Bearer token only, bodies read and written with the SCIM JSON
///     options (no MVC formatters, so application/scim+json needs no input formatter)
/// </summary>
[ServiceFilter(typeof(ScimAuthorizeFilter))]
[ApiExplorerSettings(IgnoreApi = true)]
[RequestSizeLimit(MaxBodyBytes)]
[Route("scim/v2")]
public abstract class ScimControllerBase : ControllerBase
{
    //a SCIM user or group, or a patch of one; a large group membership patch stays far below
    private const int MaxBodyBytes = 1024 * 1024;

    protected string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}/scim/v2";

    /// <summary>The body, or the 400 response to return when it is missing or is not JSON of the expected shape</summary>
    protected async Task<(T Body, IActionResult Error)> ReadBody<T>() where T : class
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<T>(Request.Body, ScimJson.Options, HttpContext.RequestAborted);
            return body == null ? (null, InvalidSyntax()) : (body, null);
        }
        catch (JsonException)
        {
            return (null, InvalidSyntax());
        }
    }

    /// <summary>The zero-based skip and the page size of a list request (startIndex is one-based)</summary>
    protected static (int StartIndex, int Skip, int Take) Page(int? startIndex, int? count, ScimConfig config)
    {
        var start = Math.Max(startIndex ?? 1, 1);
        return (start, start - 1, config.PageSize(count));
    }

    protected static IActionResult Refused(ScimOutcome outcome, string detail) => outcome switch {
        ScimOutcome.NotFound => ScimResponse.Error(StatusCodes.Status404NotFound, null, detail ?? "Resource not found"),
        ScimOutcome.Conflict => ScimResponse.Error(StatusCodes.Status409Conflict, "uniqueness", detail),
        _ => ScimResponse.Error(StatusCodes.Status400BadRequest, "invalidValue", detail)
    };

    protected static IActionResult InvalidFilter(string detail) =>
        ScimResponse.Error(StatusCodes.Status400BadRequest, "invalidFilter", detail);

    private static IActionResult InvalidSyntax() =>
        ScimResponse.Error(StatusCodes.Status400BadRequest, "invalidSyntax", "The request body is not valid SCIM JSON");
}
