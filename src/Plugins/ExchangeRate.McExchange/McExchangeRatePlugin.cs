using Grand.Infrastructure.Plugins;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Grand.Plugin.Tests")]

namespace ExchangeRate.McExchange
{
    public class McExchangeRatePlugin : BasePlugin
    {
        public McExchangeRatePlugin()
        {
        }

        public override async Task Install()
        {
            //locales
            await base.Install();
        }

        public override async Task Uninstall()
        {
            //locales
            await base.Uninstall();
        }
    }
}
