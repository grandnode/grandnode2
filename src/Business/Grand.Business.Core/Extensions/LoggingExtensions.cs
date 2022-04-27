using Grand.Domain.Customers;
using Grand.Domain.Logging;
using Grand.Business.Core.Interfaces.Common.Logging;

namespace Grand.Business.Core.Extensions
{
    public static class LoggingExtensions
    {
        public static Task Information(this ILogger logger, string message, Exception exception = null, Customer customer = null)
        {
            return FilteredLog(logger, LogLevel.Information, message, exception, customer);
        }
        public static Task Warning(this ILogger logger, string message, Exception exception = null, Customer customer = null)
        {
            return FilteredLog(logger, LogLevel.Warning, message, exception, customer);
        }
        public static Task Error(this ILogger logger, string message, Exception exception = null, Customer customer = null)
        {
            return FilteredLog(logger, LogLevel.Error, message, exception, customer);
        }
        public static Task Fatal(this ILogger logger, string message, Exception exception = null, Customer customer = null)
        {
            return FilteredLog(logger, LogLevel.Fatal, message, exception, customer);
        }

        private static Task FilteredLog(ILogger logger, LogLevel level, string message, Exception exception = null, Customer customer = null)
        {
            if (logger.IsEnabled(level))
            {
                var fullMessage = exception == null ? string.Empty : exception.ToString();
                return logger.InsertLog(level, message, fullMessage, customer);
            }
            return Task.CompletedTask;
        }
    }
}
