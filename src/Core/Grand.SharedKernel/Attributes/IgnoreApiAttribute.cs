namespace Grand.SharedKernel.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property)]
public class IgnoreApiAttribute : Attribute
{
}