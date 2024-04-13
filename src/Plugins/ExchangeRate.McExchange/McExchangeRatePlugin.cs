using Grand.Infrastructure.Plugins;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Grand.Plugin.Tests")]

namespace ExchangeRate.McExchange;

public class McExchangeRatePlugin : BasePlugin
{
}