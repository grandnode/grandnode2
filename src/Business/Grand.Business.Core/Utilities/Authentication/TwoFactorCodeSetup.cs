namespace Grand.Business.Core.Utilities.Authentication;

public class TwoFactorCodeSetup
{
    public IDictionary<string, string> CustomValues { get; set; } = new Dictionary<string, string>();
}