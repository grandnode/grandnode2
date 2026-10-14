namespace Grand.Web.Common.UserAgent;

/// <summary>
///     What the current request's User-Agent says about the client
/// </summary>
public interface IUserAgentClassifier
{
    /// <summary>
    ///     A crawler, a scraper or a plain HTTP client - including a missing User-Agent
    /// </summary>
    bool IsCrawler { get; }

    /// <summary>
    ///     A phone or a tablet
    /// </summary>
    bool IsMobileOrTablet { get; }
}
