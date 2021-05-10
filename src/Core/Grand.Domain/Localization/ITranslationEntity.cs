using System.Collections.Generic;

namespace Grand.Domain.Localization
{
    /// <summary>
    /// Represents a translation entity
    /// </summary>
    public interface ITranslationEntity
    {
        IList<TranslationEntity> Locales { get; set; }
    }
}
