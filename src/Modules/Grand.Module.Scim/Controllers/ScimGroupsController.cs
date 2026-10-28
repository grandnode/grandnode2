using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure.Configuration;
using Grand.Module.Scim.Infrastructure;
using Grand.Module.Scim.Models;
using Grand.Module.Scim.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Module.Scim.Controllers;

/// <summary>SCIM /Groups: groups of the identity provider; an administrator maps them to GrandNode roles</summary>
public class ScimGroupsController(IScimGroupService groupService, ScimConfig config) : ScimControllerBase
{
    [HttpGet("Groups")]
    public async Task<IActionResult> List([FromQuery] string filter, [FromQuery] int? startIndex, [FromQuery] int? count,
        [FromQuery] string excludedAttributes)
    {
        string displayName = null, externalId = null;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            if (!ScimFilter.TryParse(filter, out var parsed) || parsed.Attribute is not ("displayname" or "externalid"))
                return InvalidFilter("Only the filters displayName eq \"...\" and externalId eq \"...\" are supported");

            if (parsed.Value == "")
                return ScimResponse.Json(new ScimListResponse<ScimGroupResource> { StartIndex = 1 });
            if (parsed.Attribute == "displayname")
                displayName = parsed.Value;
            else
                externalId = parsed.Value;
        }

        var page = Page(startIndex, count, config);
        var result = await groupService.List(displayName, externalId, page.Skip, page.Take);
        var withMembers = !ExcludesMembers(excludedAttributes);
        return ScimResponse.Json(new ScimListResponse<ScimGroupResource> {
            TotalResults = result.Total,
            StartIndex = page.StartIndex,
            ItemsPerPage = result.Items.Count,
            Resources = result.Items.Select(x => ToResource(x, withMembers)).ToList()
        });
    }

    [HttpGet("Groups/{id}")]
    public async Task<IActionResult> Get(string id, [FromQuery] string excludedAttributes)
    {
        var result = await groupService.Get(id);
        return result.Outcome == ScimOutcome.Ok
            ? ScimResponse.Json(ToResource(result.Group, !ExcludesMembers(excludedAttributes)))
            : Refused(result.Outcome, null);
    }

    [HttpPost("Groups")]
    public async Task<IActionResult> Create()
    {
        var (body, error) = await ReadBody<ScimGroupResource>();
        if (error != null)
            return error;

        var result = await groupService.Create(ToData(body));
        if (result.Outcome != ScimOutcome.Created)
            return Refused(result.Outcome, result.Detail);

        var resource = ToResource(result.Group, true);
        Response.Headers.Location = resource.Meta.Location;
        return ScimResponse.Json(resource, StatusCodes.Status201Created);
    }

    [HttpPut("Groups/{id}")]
    public async Task<IActionResult> Replace(string id)
    {
        var (body, error) = await ReadBody<ScimGroupResource>();
        if (error != null)
            return error;

        var result = await groupService.Replace(id, ToData(body));
        return result.Outcome == ScimOutcome.Ok
            ? ScimResponse.Json(ToResource(result.Group, true))
            : Refused(result.Outcome, result.Detail);
    }

    [HttpPatch("Groups/{id}")]
    public async Task<IActionResult> Patch(string id)
    {
        var (body, error) = await ReadBody<ScimPatchRequest>();
        if (error != null)
            return error;

        if (!ScimGroupPatch.TryApply(body.Operations, out var changes, out var patchError))
            return Refused(ScimOutcome.Invalid, patchError);

        var result = await groupService.Get(id);
        if (result.Outcome == ScimOutcome.Ok && (changes.DisplayName != null || changes.ExternalId != null))
            result = await groupService.Rename(id, changes.DisplayName, changes.ExternalId);
        if (result.Outcome == ScimOutcome.Ok && changes.MembersChanged)
            result = await groupService.ModifyMembers(id, changes.Add, changes.Remove, changes.ReplaceAll);

        return result.Outcome == ScimOutcome.Ok
            ? ScimResponse.Json(ToResource(result.Group, true))
            : Refused(result.Outcome, result.Detail);
    }

    [HttpDelete("Groups/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var outcome = await groupService.Delete(id);
        return outcome == ScimOutcome.Ok ? NoContent() : Refused(outcome, null);
    }

    //Entra ID reads groups with excludedAttributes=members
    private static bool ExcludesMembers(string excludedAttributes) =>
        excludedAttributes?.Split(',').Any(x => string.Equals(x.Trim(), "members", StringComparison.OrdinalIgnoreCase)) == true;

    private static ScimGroupData ToData(ScimGroupResource body) =>
        new(body.DisplayName, body.ExternalId,
            body.Members?.Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToList() ?? []);

    private ScimGroupResource ToResource(ScimGroupView group, bool withMembers) =>
        new() {
            Schemas = [ScimSchemas.Group],
            Id = group.Id,
            ExternalId = group.ExternalId,
            DisplayName = group.DisplayName,
            Members = withMembers
                ? group.MemberIds.Select(x => new ScimMultiValue { Value = x, Ref = $"{BaseUrl}/Users/{x}" }).ToList()
                : null,
            Meta = new ScimMeta {
                ResourceType = "Group",
                Created = group.Created,
                LastModified = group.LastModified,
                Location = $"{BaseUrl}/Groups/{group.Id}"
            }
        };
}
