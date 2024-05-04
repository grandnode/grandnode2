namespace Grand.SharedKernel.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DBFieldNameAttribute : Attribute
{
    private readonly string name;

    public DBFieldNameAttribute(string name)
    {
        this.name = name;
    }

    public virtual string Name => name;
}