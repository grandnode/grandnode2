//Contribution: Orchard project (https://github.com/OrchardCMS/OrchardCore)
using Microsoft.AspNetCore.Html;

namespace Grand.Web.Common.TagHelpers
{
    public interface IResourceManager
    {
        /// <summary>
        /// Registers a script tag on the head
        /// </summary>
        /// <param name="script"></param>
        void RegisterHeadScript(IHtmlContent script, int order);

        /// <summary>
        /// Registers a custom script tag on at the header.
        /// </summary>
        void RegisterHeaderScript(IHtmlContent script, int order);

        /// <summary>
        /// Registers a custom script tag on at the foot.
        /// </summary>
        /// <param name="script"></param>
        void RegisterFootScript(IHtmlContent script, int order);

        /// <summary>
        /// Registers a custom template tag in the head/footer.
        /// </summary>
        /// <param name="script"></param>
        void RegisterTemplate(IHtmlContent script, bool head);

        /// <summary>
        /// Returns the registered head script resources.
        /// </summary>
        IEnumerable<IHtmlContent> GetRegisteredHeadScripts();

        /// <summary>
        /// Returns the registered header script resources.
        /// </summary>
        IEnumerable<IHtmlContent> GetRegisteredHeaderScripts();

        /// <summary>
        /// Returns the registered footer script resources.
        /// </summary>
        IEnumerable<IHtmlContent> GetRegisteredFootScripts();

        /// <summary>
        /// Renders the registered header script tags.
        /// </summary>
        void RenderHeadScript(IHtmlContentBuilder builder);

        /// <summary>
        /// Renders the registered header script tags.
        /// </summary>
        void RenderHeaderScript(IHtmlContentBuilder builder);

        /// <summary>
        /// Renders the registered footer script tags.
        /// </summary>
        void RenderFootScript(IHtmlContentBuilder builder);

        /// <summary>
        /// Renders the registered header link tags.
        /// </summary>
        void RenderHeadLink(IHtmlContentBuilder builder);

        /// <summary>
        /// Renders the registered template tags.
        /// </summary>
        void RenderTemplate(IHtmlContentBuilder builder, bool head);

        /// <summary>
        /// Registers a link tag.
        /// </summary>
        void RegisterLink(LinkEntry link);
    }
}
