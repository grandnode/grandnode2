namespace Grand.Infrastructure.TypeConverters;

public interface ITypeConverter
{
    /// <summary>
    ///     Gets order of this configuration implementation
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     Register converter
    /// </summary>
    void Register();
}