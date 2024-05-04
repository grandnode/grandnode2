using Grand.Business.Core.Interfaces.Authentication;

namespace Grand.Business.Authentication.Tests;

public class ExternalAuthenticationProviderTest : IExternalAuthenticationProvider
{
    public string ConfigurationUrl => "";

    public string SystemName => nameof(ExternalAuthenticationProviderTest);

    public string FriendlyName => nameof(ExternalAuthenticationProviderTest);

    public int Priority => 0;

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    public Task<string> GetPublicViewComponentName()
    {
        throw new NotImplementedException();
    }
}