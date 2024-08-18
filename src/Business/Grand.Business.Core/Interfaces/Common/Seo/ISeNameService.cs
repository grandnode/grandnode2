using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.Infrastructure.Models;
using System.Linq.Expressions;

namespace Grand.Business.Core.Interfaces.Common.Seo;

public interface ISeNameService
{
    Task<string> ValidateSeName<T>(T entity, string seName, string name, bool ensureNotEmpty) where T : BaseEntity, ISlugEntity;
    Task<List<TranslationEntity>> TranslationSeNameProperties<T, TE>(IList<T> list, TE entity, Expression<Func<T, string>> keySelector) 
        where T : ILocalizedModelLocal where TE : BaseEntity, ISlugEntity;
    Task SaveSeName<T>(T entity) where T : BaseEntity, ISlugEntity;
}