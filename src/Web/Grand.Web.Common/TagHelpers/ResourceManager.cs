//Contribution: Orchard project (https://github.com/OrchardCMS/OrchardCore)
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;

namespace Grand.Web.Common.TagHelpers
{
    public class ResourceManager : IResourceManager
    {
        private List<IHtmlContent> _headScripts;
        private List<IHtmlContent> _headerScripts;
        private List<IHtmlContent> _footScripts;
        private List<LinkEntry> _links;

        public ResourceManager()
        {
            _links = new List<LinkEntry>();
        }
        public IEnumerable<IHtmlContent> GetRegisteredHeadScripts()
        {
            return _headScripts == null ? Enumerable.Empty<IHtmlContent>() : _headScripts;
        }
        public IEnumerable<IHtmlContent> GetRegisteredHeaderScripts()
        {
            return _headerScripts == null ? Enumerable.Empty<IHtmlContent>() : _headerScripts;
        }

        public IEnumerable<IHtmlContent> GetRegisteredFootScripts()
        {
            return _footScripts == null ? Enumerable.Empty<IHtmlContent>() : _footScripts;
        }

        public void RegisterHeadScript(IHtmlContent script)
        {
            if (_headScripts == null)
            {
                _headScripts = new List<IHtmlContent>();
            }
            _headScripts.Add(script);
        }
        public void RegisterHeaderScript(IHtmlContent script)
        {
            if (_headerScripts == null)
            {
                _headerScripts = new List<IHtmlContent>();
            }

            _headerScripts.Add(script);
        }

        public void RegisterFootScript(IHtmlContent script)
        {
            if (_footScripts == null)
            {
                _footScripts = new List<IHtmlContent>();
            }

            _footScripts.Add(script);
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

        public void RegisterLink(LinkEntry link)
        {
            _links.Add(link);
        }
    }
}
