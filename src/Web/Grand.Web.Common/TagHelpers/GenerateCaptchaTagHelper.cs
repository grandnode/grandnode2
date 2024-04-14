using Grand.Web.Common.Security.Captcha;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers;

[HtmlTargetElement("captcha", TagStructure = TagStructure.WithoutEndTag)]
public class GenerateCaptchaTagHelper : TagHelper
{
    private readonly CaptchaSettings _captchaSettings;
    private readonly IHtmlHelper _htmlHelper;

    public GenerateCaptchaTagHelper(IHtmlHelper htmlHelper, CaptchaSettings captchaSettings)
    {
        _htmlHelper = htmlHelper;
        _captchaSettings = captchaSettings;
    }

    /// <summary>
    ///     ViewContext
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        //contextualize IHtmlHelper
        var viewContextAware = _htmlHelper as IViewContextAware;
        viewContextAware?.Contextualize(ViewContext);

        //generate captcha control
        var captchaControl = new GoogleRecaptchaControl(_captchaSettings.ReCaptchaVersion) {
            Theme = _captchaSettings.ReCaptchaTheme,
            Id = "g-recaptcha-response-value",
            PublicKey = _captchaSettings.ReCaptchaPublicKey,
            Language = _captchaSettings.ReCaptchaLanguage
        };
        var captchaControlHtml = captchaControl.RenderControl();

        //tag details
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(captchaControlHtml);
    }
}