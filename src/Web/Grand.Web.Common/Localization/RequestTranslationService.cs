using Grand.Business.Common.Services.Localization;
using Grand.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Mediator;

namespace Grand.Web.Common.Localization;

public class RequestTranslationService(
    IRepository<TranslationResource> trRepository,
    IMediator mediator,
    IContextAccessor contextAccessor)
    : TranslationService(trRepository, mediator), IRequestTranslationService
{
    public string GetResource(string name)
    {
        var language = contextAccessor.WorkContext?.WorkingLanguage;
        return language != null ? GetResource(name, language.Id) : "";
    }
}
