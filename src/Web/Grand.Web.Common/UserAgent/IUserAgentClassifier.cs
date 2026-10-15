namespace Grand.Web.Common.UserAgent;

/// <summary>
///     What the current request's User-Agent says about the client
/// </summary>
public interface IUserAgentClassifier
{
    /// <summary>
    ///     A crawler identified by its User-Agent. A missing User-Agent or a plain HTTP client is not a crawler
    /// </summary>
    bool IsCrawler { get; }

    /// <summary>
    ///     A phone or a tablet
    /// </summary>
    bool IsMobileOrTablet { get; }
}
