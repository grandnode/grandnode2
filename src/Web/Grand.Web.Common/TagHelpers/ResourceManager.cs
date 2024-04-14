//Contribution: Orchard project (https://github.com/OrchardCMS/OrchardCore)

using Microsoft.AspNetCore.Html;

namespace Grand.Web.Common.TagHelpers;

public class ResourceManager : IResourceManager
{
    private readonly List<(IHtmlContent content, int order)> _footScripts = new();
    private readonly List<(IHtmlContent content, int order)> _headerScripts = new();
    private readonly List<(IHtmlContent content, int order)> _headScripts = new();
    private readonly List<LinkEntry> _links = new();
    private List<IHtmlContent> _templatesFooter;
    private List<IHtmlContent> _templatesHeader;

    public IEnumerable<IHtmlContent> GetRegisteredHeadScripts()
    {
        return _headScripts.OrderBy(x => x.order).Select(x => x.content);
    }

    public IEnumerable<IHtmlContent> GetRegisteredHeaderScripts()
    {
        return _headerScripts.OrderBy(x => x.order).Select(x => x.content);
    }

    public IEnumerable<IHtmlContent> GetRegisteredFootScripts()
    {
        return _footScripts.OrderBy(x => x.order).Select(x => x.content);
    }

    public void RegisterHeadScript(IHtmlContent script, int order)
    {
        _headScripts.Add((script, order));
    }

    public void RegisterHeaderScript(IHtmlContent script, int order)
    {
        _headerScripts.Add((script, order));
    }

    public void RegisterFootScript(IHtmlContent script, int order)
    {
        _footScripts.Add((script, order));
    }

    public void RegisterTemplate(IHtmlContent script, bool head)
    {
        if (head)
        {
            _templatesHeader ??= new List<IHtmlContent>();
            _templatesHeader.Add(script);
        }
        else
        {
            _templatesFooter ??= new List<IHtmlContent>();
            _templatesFooter.Add(script);
        }
    }

    /// <summary>
    ///     Renders the registered head script tags.
    /// </summary>
    public void RenderHeadScript(IHtmlContentBuilder builder)
    {
        var first = true;
        foreach (var context in GetRegisteredHeadScripts())
        {
            if (!first) builder.AppendHtml(Environment.NewLine);

            first = false;

            builder.AppendHtml(context);
        }
    }

    public void RenderFootScript(IHtmlContentBuilder builder)
    {
        var first = true;
        foreach (var context in GetRegisteredFootScripts())
        {
            if (!first) builder.AppendHtml(Environment.NewLine);

            first = false;

            builder.AppendHtml(context);
        }
    }

    public void RenderHeaderScript(IHtmlContentBuilder builder)
    {
        var first = true;
        foreach (var context in GetRegisteredHeaderScripts())
        {
            if (!first) builder.AppendHtml(Environment.NewLine);

            first = false;

            builder.AppendHtml(context);
        }
    }


    public void RenderHeadLink(IHtmlContentBuilder builder)
    {
        var first = true;

        var registeredLinks = _links.OrderBy(x => x.Priority).ToList();
        foreach (var link in registeredLinks)
        {
            if (!first) builder.AppendHtml(Environment.NewLine);

            first = false;

            builder.AppendHtml(link.GetTag());
        }
    }

    public void RenderTemplate(IHtmlContentBuilder builder, bool head)
    {
        var first = true;
        var templates = head ? GetRegisteredTemplatesHeader() : GetRegisteredTemplatesFooter();
        foreach (var context in templates)
        {
            if (!first) builder.AppendHtml(Environment.NewLine);

            first = false;

            builder.AppendHtml(context);
        }
    }

    public void RegisterLink(LinkEntry link)
    {
        _links.Add(link);
    }

    public IEnumerable<IHtmlContent> GetRegisteredTemplatesHeader()
    {
        return _templatesHeader ?? Enumerable.Empty<IHtmlContent>();
    }

    public IEnumerable<IHtmlContent> GetRegisteredTemplatesFooter()
    {
        return _templatesFooter ?? Enumerable.Empty<IHtmlContent>();
    }
}