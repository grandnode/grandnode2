namespace Grand.Business.Core.Utilities.Messages;

/// <summary>
///     Rendered subject and body of a newsletter campaign
/// </summary>
public sealed record CampaignMessage(string Subject, string Body);
