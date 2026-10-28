using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Grand.Module.Scim.Infrastructure;
using Grand.Module.Scim.Models;
using Grand.Module.Scim.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Module.Scim.Controllers;

/// <summary>SCIM /Users: staff accounts provisioned by the identity provider</summary>
public class ScimUsersController(IScimUserService userService, ScimConfig config) : ScimControllerBase
{
    [HttpGet("Users")]
    public async Task<IActionResult> List([FromQuery] string filter, [FromQuery] int? startIndex, [FromQuery] int? count)
    {
        string userName = null, externalId = null;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            if (!ScimFilter.TryParse(filter, out var parsed) || parsed.Attribute is not ("username" or "externalid"))
                return InvalidFilter("Only the filters userName eq \"...\" and externalId eq \"...\" are supported");

            //an empty value matches no user (and must not drop the filter)
            if (parsed.Value == "")
                return ScimResponse.Json(new ScimListResponse<ScimUserResource> { StartIndex = 1 });
            if (parsed.Attribute == "username")
                userName = parsed.Value;
            else
                externalId = parsed.Value;
        }

        var page = Page(startIndex, count, config);
        var result = await userService.List(userName, externalId, page.Skip, page.Take);
        return ScimResponse.Json(new ScimListResponse<ScimUserResource> {
            TotalResults = result.Total,
            StartIndex = page.StartIndex,
            ItemsPerPage = result.Items.Count,
            Resources = result.Items.Select(ToResource).ToList()
        });
    }

    [HttpGet("Users/{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await userService.Get(id);
        return result.Outcome == ScimOutcome.Ok ? ScimResponse.Json(ToResource(result.User)) : Refused(result.Outcome, null);
    }

    [HttpPost("Users")]
    public async Task<IActionResult> Create()
    {
        var (body, error) = await ReadBody<ScimUserResource>();
        if (error != null)
            return error;

        var result = await userService.Create(ToData(body, true));
        if (result.Outcome != ScimOutcome.Created)
            return Refused(result.Outcome, result.Detail);

        var resource = ToResource(result.User);
        Response.Headers.Location = resource.Meta.Location;
        return ScimResponse.Json(resource, StatusCodes.Status201Created);
    }

    [HttpPut("Users/{id}")]
    public async Task<IActionResult> Replace(string id)
    {
        var (body, error) = await ReadBody<ScimUserResource>();
        if (error != null)
            return error;

        var current = await userService.Get(id);
        if (current.Outcome != ScimOutcome.Ok)
            return Refused(current.Outcome, null);

        //a replace without active keeps the state: it must not activate a deactivated account
        var result = await userService.Replace(id, ToData(body, current.User.Active));
        return result.Outcome == ScimOutcome.Ok
            ? ScimResponse.Json(ToResource(result.User))
            : Refused(result.Outcome, result.Detail);
    }

    [HttpPatch("Users/{id}")]
    public async Task<IActionResult> Patch(string id)
    {
        var (body, error) = await ReadBody<ScimPatchRequest>();
        if (error != null)
            return error;

        var current = await userService.Get(id);
        if (current.Outcome != ScimOutcome.Ok)
            return Refused(current.Outcome, null);

        var user = current.User;
        var data = new ScimUserData(user.UserName, user.ExternalId, user.Email, user.GivenName, user.FamilyName, user.Active);
        if (!ScimUserPatch.TryApply(data, body.Operations, out var patched, out var patchError))
            return Refused(ScimOutcome.Invalid, patchError);

        var result = await userService.Replace(id, patched);
        return result.Outcome == ScimOutcome.Ok
            ? ScimResponse.Json(ToResource(result.User))
            : Refused(result.Outcome, result.Detail);
    }

    [HttpDelete("Users/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var outcome = await userService.Delete(id);
        return outcome == ScimOutcome.Ok ? NoContent() : Refused(outcome, null);
    }

    private static ScimUserData ToData(ScimUserResource body, bool activeWhenMissing) =>
        new(body.UserName, body.ExternalId, PickEmail(body.Emails), body.Name?.GivenName, body.Name?.FamilyName,
            body.Active ?? activeWhenMissing);

    //the primary e-mail, else the work e-mail, else the first one
    private static string PickEmail(List<ScimMultiValue> emails) =>
        (emails?.FirstOrDefault(x => x.Primary == true) ??
         emails?.FirstOrDefault(x => string.Equals(x.Type, "work", StringComparison.OrdinalIgnoreCase)) ??
         emails?.FirstOrDefault())?.Value;

    private ScimUserResource ToResource(ScimUserView user)
    {
        var displayName = $"{user.GivenName} {user.FamilyName}".Trim();
        return new ScimUserResource {
            Schemas = [ScimSchemas.User],
            Id = user.Id,
            ExternalId = user.ExternalId,
            UserName = user.UserName,
            Name = string.IsNullOrEmpty(user.GivenName) && string.IsNullOrEmpty(user.FamilyName)
                ? null
                : new ScimName { GivenName = user.GivenName, FamilyName = user.FamilyName, Formatted = displayName },
            DisplayName = displayName == "" ? null : displayName,
            Emails = string.IsNullOrEmpty(user.Email)
                ? null
                : [new ScimMultiValue { Value = user.Email, Type = "work", Primary = true }],
            Active = user.Active,
            Groups = user.Groups.Select(x => new ScimMultiValue {
                Value = x.Id, Display = x.DisplayName, Ref = $"{BaseUrl}/Groups/{x.Id}"
            }).ToList(),
            Meta = new ScimMeta {
                ResourceType = "User",
                Created = user.Created,
                LastModified = user.LastModified,
                Location = $"{BaseUrl}/Users/{user.Id}"
            }
        };
    }
}
