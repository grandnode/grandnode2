#nullable enable

using System.Linq.Expressions;

namespace Grand.Mapping.Internal;

internal sealed class MemberConfig
{
    public bool IsIgnored { get; set; }
    public LambdaExpression? MapFromExpression { get; set; }
    public LambdaExpression? ConditionExpression { get; set; }
    public string MemberName { get; set; } = "";
    public LambdaExpression? DestinationPathExpression { get; set; }
    public bool IsPath { get; set; }
}
