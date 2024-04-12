using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Domain.Directory;

/// <summary>
///     Represents a country
/// </summary>
public class Country : BaseEntity, ITranslationEntity, IStoreLinkEntity
{
    private ICollection<StateProvince> _stateProvinces;

    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether billing is allowed to this country
    /// </summary>
    public bool AllowsBilling { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether shipping is allowed to this country
    /// </summary>
    public bool AllowsShipping { get; set; }

    /// <summary>
    ///     Gets or sets the two letter ISO code
    /// </summary>
    public string TwoLetterIsoCode { get; set; }

    /// <summary>
    ///     Gets or sets the three letter ISO code
    /// </summary>
    public string ThreeLetterIsoCode { get; set; }

    /// <summary>
    ///     Gets or sets the numeric ISO code
    /// </summary>
    public int NumericIsoCode { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether customers in this country must be charged EU VAT
    /// </summary>
    public bool SubjectToVat { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is published
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets the state/provinces
    /// </summary>
    public virtual ICollection<StateProvince> StateProvinces {
        get => _stateProvinces ??= new List<StateProvince>();
        protected set => _stateProvinces = value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    public IList<string> Stores { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets the collection of locales
    /// </summary>
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
}