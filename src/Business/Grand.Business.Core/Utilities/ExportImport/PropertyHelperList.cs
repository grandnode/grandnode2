namespace Grand.Business.Core.Utilities.ExportImport
{
    public class PropertyHelperList<T>
    {
        private static T obj;

        public PropertyHelperList(T obj1)
        {
            ObjectList = new List<PropertyHelperList<T>>();
            obj = obj1;
        }
        public List<PropertyHelperList<T>> ObjectList { get; set; }

        private string PropertyName { get; set; }
        private object PropertyValue { get; set; }

        public PropertyHelperList(string propertyName, Func<T, object> func = null)
        {
            PropertyName = propertyName;
            PropertyValue = func(obj);
        }

        public List<PropertyList> ToList()
        {
            List<PropertyList> list = new List<PropertyList>();
            foreach (var item in ObjectList)
            {
                list.Add(new PropertyList(item.PropertyName, item.PropertyValue?.ToString()));
            }

            return list;
        }

        public class PropertyList
        {
            public PropertyList(string field, string value)
            {
                Field = field;
                Value = value;
            }
            public string Field { get; set; }
            public string Value { get; set; }
        }        
    }
}
