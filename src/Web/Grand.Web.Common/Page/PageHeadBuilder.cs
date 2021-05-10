using Grand.Domain.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grand.Web.Common.Page
{
    /// <summary>
    /// Page head builder
    /// </summary>
    public partial class PageHeadBuilder : IPageHeadBuilder
    {
        #region Fields

        private readonly List<string> _title;
        private readonly List<string> _metaDescription;
        private readonly List<string> _metaKeyword;
        private readonly List<string> _canonicalUrl;
        private readonly List<string> _headCustom;
        private readonly List<string> _pageCssClass;
        private string _editPageUrl;

        private readonly SeoSettings _seoSettings;
        #endregion

        #region Ctor

        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="seoSettings">SEO settings</param>
        /// <param name="hostingEnvironment">Hosting environment</param>
        public PageHeadBuilder(SeoSettings seoSettings)
        {
            _title = new List<string>();
            _metaDescription = new List<string>();
            _metaKeyword = new List<string>();
            _canonicalUrl = new List<string>();
            _headCustom = new List<string>();
            _pageCssClass = new List<string>();

            if (!string.IsNullOrEmpty(seoSettings.CustomHeadTags))
            {
                AppendHeadCustomParts(seoSettings.CustomHeadTags);
            }
            _seoSettings = seoSettings;
        }

        #endregion

        #region Methods

        public virtual void AddTitleParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _title.Add(part);
        }
        public virtual void AppendTitleParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _title.Insert(0, part);
        }
        public virtual string GenerateTitle(bool addDefaultTitle)
        {
            string result;
            var titleParts = string.Join(_seoSettings.PageTitleSeparator, _title.AsEnumerable().Reverse().ToList());
            if (!String.IsNullOrEmpty(titleParts))
            {
                if (addDefaultTitle)
                {
                    if (_seoSettings.PageTitleSeoAdjustment)
                        result = string.Join(_seoSettings.PageTitleSeparator, _seoSettings.DefaultTitle, titleParts);
                    else
                        result = string.Join(_seoSettings.PageTitleSeparator, titleParts, _seoSettings.DefaultTitle);
                }
                else
                {
                    result = titleParts;
                }
            }
            else
            {
                result = _seoSettings.DefaultTitle;
            }
            return result;
        }

        public virtual void AddMetaDescriptionParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _metaDescription.Add(part);
        }

        public virtual void AppendMetaDescriptionParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _metaDescription.Insert(0, part);
        }

        public virtual string GenerateMetaDescription()
        {
            var description = string.Join(", ", _metaDescription.AsEnumerable().Reverse().ToList());
            var result = !String.IsNullOrEmpty(description) ? description : _seoSettings.DefaultMetaDescription;
            return result;
        }

        public virtual void AddMetaKeywordParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _metaKeyword.Add(part);
        }

        public virtual void AppendMetaKeywordParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _metaKeyword.Insert(0, part);
        }

        public virtual string GenerateMetaKeywords()
        {
            var keyword = string.Join(", ", _metaKeyword.AsEnumerable().Reverse().ToList());
            var result = !String.IsNullOrEmpty(keyword) ? keyword : _seoSettings.DefaultMetaKeywords;
            return result;
        }

        public virtual void AddCanonicalUrlParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _canonicalUrl.Add(part);
        }

        public virtual void AppendCanonicalUrlParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _canonicalUrl.Insert(0, part);
        }

        public virtual string GenerateCanonicalUrls()
        {
            var result = new StringBuilder();
            foreach (var canonicalUrl in _canonicalUrl)
            {
                result.AppendFormat("<link rel=\"canonical\" href=\"{0}\" />", canonicalUrl);
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        public virtual void AddHeadCustomParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _headCustom.Add(part);
        }

        public virtual void AppendHeadCustomParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _headCustom.Insert(0, part);
        }

        public virtual string GenerateHeadCustom()
        {
            var distinctParts = _headCustom.Distinct().ToList();
            if (!distinctParts.Any())
                return "";

            var result = new StringBuilder();
            foreach (var path in distinctParts)
            {
                result.Append(path);
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        public virtual void AddPageCssClassParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _pageCssClass.Add(part);
        }

        public virtual void AppendPageCssClassParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _pageCssClass.Insert(0, part);
        }

        public virtual string GeneratePageCssClasses()
        {
            string result = string.Join(" ", _pageCssClass.AsEnumerable().Reverse().ToArray());
            return result;
        }

        public virtual string EditPageUrl
        {
            get { return _editPageUrl; }   
            set { _editPageUrl = value; }  
        }

        #endregion
    }
}