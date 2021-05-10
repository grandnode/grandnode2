using Grand.Infrastructure.Plugins;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Grand.Plugin.Tests")]

namespace Grand.Plugin.ExchangeRate.McExchange
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
