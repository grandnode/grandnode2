using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Localization;
using Grand.Domain.Pages;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Pages;
using Grand.Web.Models.Pages;
using MediatR;

namespace Grand.Web.Features.Handlers.Pages;

public class GetPageBlockHandler : IRequestHandler<GetPageBlock, PageModel>
{
    private readonly IAclService _aclService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPageService _pageService;
    private readonly IContextAccessor _contextAccessor;

    public GetPageBlockHandler(
        IPageService pageService,
        IContextAccessor contextAccessor,
        IAclService aclService,
        IDateTimeService dateTimeService)
    {
        _pageService = pageService;
        _contextAccessor = contextAccessor;
        _aclService = aclService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PageModel> Handle(GetPageBlock request, CancellationToken cancellationToken)
    {
        //load by store
        var page = string.IsNullOrEmpty(request.PageId)
            ? await _pageService.GetPageBySystemName(request.SystemName, _contextAccessor.StoreContext.CurrentStore.Id)
            : await _pageService.GetPageById(request.PageId);

        if (page is not { Published: true })
            return null;

        if ((page.StartDateUtc.HasValue && page.StartDateUtc > DateTime.UtcNow) ||
            (page.EndDateUtc.HasValue && page.EndDateUtc < DateTime.UtcNow))
            return null;

        //ACL (access control list)
        return !_aclService.Authorize(page, _contextAccessor.WorkContext.CurrentCustomer)
            ? null
            : ToModel(page, _contextAccessor.WorkContext.WorkingLanguage, request.Password);
    }
    
    private PageModel ToModel(Page entity, Language language,
        string password = "")
    {
        var model = new PageModel {
            Id = entity.Id,
            SystemName = entity.SystemName,
            IncludeInSitemap = entity.IncludeInSitemap,
            IsPasswordProtected = entity.IsPasswordProtected,
            Password = entity.Password == password ? password : "",
            Title = entity.IsPasswordProtected && entity.Password != password
                ? ""
                : entity.GetTranslation(x => x.Title, language.Id),
            Body = entity.IsPasswordProtected && entity.Password != password
                ? ""
                : entity.GetTranslation(x => x.Body, language.Id),
            MetaKeywords = entity.GetTranslation(x => x.MetaKeywords, language.Id),
            MetaDescription = entity.GetTranslation(x => x.MetaDescription, language.Id),
            MetaTitle = entity.GetTranslation(x => x.MetaTitle, language.Id),
            SeName = entity.GetSeName(language.Id),
            PageLayoutId = entity.PageLayoutId,
            Published = entity.Published,
            StartDate = entity.StartDateUtc.HasValue
                ? _dateTimeService.ConvertToUserTime(entity.StartDateUtc.Value)
                : default,
            EndDate = entity.EndDateUtc.HasValue ? _dateTimeService.ConvertToUserTime(entity.EndDateUtc.Value) : default
        };
        return model;
    }

}