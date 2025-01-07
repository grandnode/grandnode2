namespace Grand.SharedKernel.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ApiGroupAttribute : Attribute
{
    public string GroupName { get; }

    public ApiGroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
}