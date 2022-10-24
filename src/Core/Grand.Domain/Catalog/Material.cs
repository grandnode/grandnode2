using Grand.Domain.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Domain.Catalog
{
    public class Material : SubBaseEntity, ITranslationEntity
    {
        public string Name { get; set; }
        public string FilePath { get; set; }

        public decimal Cost { get; set; }
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the collection of locales
        /// </summary>
        public IList<TranslationEntity> Locales { get; set; }
    }
}
