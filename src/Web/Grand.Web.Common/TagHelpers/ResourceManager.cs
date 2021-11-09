//Contribution: Orchard project (https://github.com/OrchardCMS/OrchardCore)
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;

namespace Grand.Web.Common.TagHelpers
{
    public class ResourceManager : IResourceManager
    {
        private List<(IHtmlContent content, int order)> _headScripts;
        private List<(IHtmlContent content, int order)> _headerScripts;
        private List<(IHtmlContent content, int order)> _footScripts;
        private List<IHtmlContent> _templates;
        private List<LinkEntry> _links;

        public ResourceManager()
        {
            _links = new List<LinkEntry>();
            _headScripts = new List<(IHtmlContent content, int order)>();
            _headerScripts = new List<(IHtmlContent content, int order)>();
            _footScripts = new List<(IHtmlContent content, int order)>();
        }
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
        public IEnumerable<IHtmlContent> GetRegisteredTemplates()
        {
            return _templates == null ? Enumerable.Empty<IHtmlContent>() : _templates;
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
        public void RegisterTemplate(IHtmlContent script)
        {
            if (_templates == null)
            {
                _templates = new List<IHtmlContent>();
            }

            _templates.Add(script);
        }
        /// <summary>
        /// Renders the registered head script tags.
        /// </summary>
        public void RenderHeadScript(IHtmlContentBuilder builder)
        {
            var first = true;
            foreach (var context in GetRegisteredHeadScripts())
            {
                if (!first)
                {
                    builder.AppendHtml(Environment.NewLine);
                }

                first = false;

                builder.AppendHtml(context);
            }
        }

        public void RenderFootScript(IHtmlContentBuilder builder)
        {
            var first = true;
            foreach (var context in GetRegisteredFootScripts())
            {
                if (!first)
                {
                    builder.AppendHtml(Environment.NewLine);
                }

                first = false;

                builder.AppendHtml(context);
            }
        }
        public void RenderHeaderScript(IHtmlContentBuilder builder)
        {
            var first = true;
            foreach (var context in GetRegisteredHeaderScripts())
            {
                if (!first)
                {
                    builder.AppendHtml(Environment.NewLine);
                }

                first = false;

                builder.AppendHtml(context);
            }

        }


        public void RenderHeadLink(IHtmlContentBuilder builder)
        {
            var first = true;

            var registeredLinks = _links.OrderBy(x => x.Priority).ToList();
            for (var i = 0; i < registeredLinks.Count; i++)
            {
                var link = registeredLinks[i];
                if (!first)
                {
                    builder.AppendHtml(System.Environment.NewLine);
                }

                first = false;

                builder.AppendHtml(link.GetTag());
            }
        }
        public void RenderTemplate(IHtmlContentBuilder builder)
        {
            var first = true;
            foreach (var context in GetRegisteredTemplates())
            {
                if (!first)
                {
                    builder.AppendHtml(Environment.NewLine);
                }

                first = false;

                builder.AppendHtml(context);
            }
        }
        public void RegisterLink(LinkEntry link)
        {
            _links.Add(link);
        }
    }
}
