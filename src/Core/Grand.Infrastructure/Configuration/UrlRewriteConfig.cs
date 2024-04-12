namespace Grand.Infrastructure.Configuration;

public class UrlRewriteConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether we use url rewrite
    /// </summary>
    public bool UseUrlRewrite { get; set; }

    public bool UrlRewriteHttpsOptions { get; set; }
    public int UrlRewriteHttpsOptionsStatusCode { get; set; }
    public int UrlRewriteHttpsOptionsPort { get; set; }
    public bool UrlRedirectToHttpsPermanent { get; set; }
}