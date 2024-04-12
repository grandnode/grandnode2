namespace Grand.Business.Core.Utilities.ExportImport;

/// <summary>
///     Class for working with PropertyByName object list
/// </summary>
/// <typeparam name="T">Object type</typeparam>
public class PropertyManager<T>
{
    /// <summary>
    ///     All properties
    /// </summary>
    private readonly Dictionary<string, PropertyByName<T>> _properties;

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="properties">All access properties</param>
    public PropertyManager(PropertyByName<T>[] properties)
    {
        _properties = new Dictionary<string, PropertyByName<T>>();

        var poz = 0;
        foreach (var propertyByName in properties)
        {
            propertyByName.PropertyOrderPosition = poz;
            poz++;
            _properties.Add(propertyByName.PropertyName, propertyByName);
        }
    }
}