using Grand.SharedKernel.Extensions;

namespace Grand.Domain.Common;

public static class UserFieldExtensions
{
    /// <summary>
    ///     Get an attribute of an entity
    /// </summary>
    /// <typeparam name="TPropType">Property type</typeparam>
    /// <param name="entity">Entity</param>
    /// <param name="key">Key</param>
    /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
    /// <returns>Attribute</returns>
    public static TPropType GetUserFieldFromEntity<TPropType>(this BaseEntity entity, string key, string storeId = "")
    {
        ArgumentNullException.ThrowIfNull(entity);

        var props = entity.UserFields;
        if (props == null)
            return default;
        props = props.Where(x => x.StoreId == storeId).ToList();
        if (!props.Any())
            return default;

        var prop = props.FirstOrDefault(ga =>
            ga.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

        if (prop == null || string.IsNullOrEmpty(prop.Value))
            return default;

        return CommonHelper.To<TPropType>(prop.Value);
    }
}