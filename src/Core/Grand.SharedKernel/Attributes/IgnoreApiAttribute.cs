namespace Grand.SharedKernel.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
public class IgnoreApiAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
public class IgnoreApiUrlAttribute : Attribute
{
}