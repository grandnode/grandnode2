//Contribution https://github.com/smartstore/SmartStoreNET/blob/2.0.x/src/Presentation/SmartStore.Web.Framework/UI/IPageAssetsBuilder.cs

namespace Grand.Web.Common.Page
{
    /// <summary>
    /// Page head builder
    /// </summary>
    public partial interface IPageHeadBuilder
    {
        void AddTitleParts(string part);
        void AppendTitleParts(string part);
        string GenerateTitle(bool addDefaultTitle);
        void AddMetaDescriptionParts(string part);
        void AppendMetaDescriptionParts(string part);
        string GenerateMetaDescription();

        void AddMetaKeywordParts(string part);
        void AppendMetaKeywordParts(string part);
        string GenerateMetaKeywords();

        void AddCanonicalUrlParts(string part);
        void AppendCanonicalUrlParts(string part);
        string GenerateCanonicalUrls();

        void AddHeadCustomParts(string part);
        void AppendHeadCustomParts(string part);
        string GenerateHeadCustom();

        void AddPageCssClassParts(string part);
        void AppendPageCssClassParts(string part);
        string GeneratePageCssClasses();

        string EditPageUrl { get; set; }

    }
}
