using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.UserAgent;

/// <summary>
///     Classifies the current request's User-Agent once per request
/// </summary>
public class UserAgentClassifier(IHttpContextAccessor httpContextAccessor) : IUserAgentClassifier
{
    //user agents longer than this are cut before matching
    private const int MaxLength = 512;

    //"bot" not preceded by "cu": CUBOT is an Android phone brand
    private static readonly Regex CrawlerPattern = new(
        "(?<!cu)bot|crawl|spider|slurp|headless|facebookexternalhit|meta-externalagent|scrapy|curl/|wget/|python-|" +
        "go-http-client|okhttp|java/|httpclient|libwww|axios/|node-fetch",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex MobileOrTabletPattern = new(
        "mobi|android|iphone|ipad|ipod|tablet|silk|kindle",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private bool? _isCrawler;
    private bool? _isMobileOrTablet;

    public bool IsCrawler => _isCrawler ??= Classify(userAgent =>
        string.IsNullOrWhiteSpace(userAgent) || CrawlerPattern.IsMatch(userAgent));

    public bool IsMobileOrTablet => _isMobileOrTablet ??= Classify(userAgent =>
        !string.IsNullOrWhiteSpace(userAgent) && MobileOrTabletPattern.IsMatch(userAgent));

    private bool Classify(Func<string, bool> rule)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null) return false;

        var userAgent = request.Headers.UserAgent.ToString();
        if (userAgent.Length > MaxLength) userAgent = userAgent[..MaxLength];
        return rule(userAgent);
    }
}
