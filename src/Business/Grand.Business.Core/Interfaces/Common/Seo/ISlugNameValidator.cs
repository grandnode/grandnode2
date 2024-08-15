using Grand.Domain;
using Grand.Domain.Seo;

namespace Grand.Business.Core.Interfaces.Common.Seo;

public interface ISlugNameValidator 
{
    Task<string> ValidateSeName<T>(T entity, string seName, string name, bool ensureNotEmpty) where T : BaseEntity, ISlugEntity;
}