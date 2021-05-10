namespace Grand.Infrastructure.TypeConverters
{
    public interface ITypeConverter
    {
        /// <summary>
        /// Register converter
        /// </summary>
        void Register();

        /// <summary>
        /// Gets order of this configuration implementation
        /// </summary>
        int Order { get; }
    }
}
