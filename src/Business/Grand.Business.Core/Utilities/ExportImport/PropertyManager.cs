namespace Grand.Business.Core.Utilities.ExportImport
{
    /// <summary>
    /// Class for working with PropertyByName object list
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    public class PropertyManager<T>
    {
        /// <summary>
        /// All properties
        /// </summary>
        private readonly Dictionary<string, PropertyByName<T>> _properties;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="properties">All acsess properties</param>
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

        /// <summary>
        /// Curent object to acsess
        /// </summary>
        public T CurrentObject { get; set; }

        /// <summary>
        /// Return properti index
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns></returns>
        public int GetIndex(string propertyName)
        {
            if (!_properties.ContainsKey(propertyName))
                return -1;

            return _properties[propertyName].PropertyOrderPosition;
        }

        /// <summary>
        /// Access object by property name
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Property value</returns>
        public object this[string propertyName] {
            get {
                return _properties.ContainsKey(propertyName) && CurrentObject != null
                    ? _properties[propertyName].GetProperty(CurrentObject)
                    : null;
            }
        }
        
        /// <summary>
        /// Count of properties
        /// </summary>
        public int Count {
            get { return _properties.Count; }
        }

        /// <summary>
        /// Get property by name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public PropertyByName<T> GetProperty(string propertyName)
        {
            return _properties.ContainsKey(propertyName) ? _properties[propertyName] : null;
        }


        /// <summary>
        /// Get property array
        /// </summary>
        public PropertyByName<T>[] GetProperties {
            get { return _properties.Values.ToArray(); }
        }
    }
}