using Core.Interfaces;
using Serilog;

namespace Application.Services
{
    public class LoggingService : ILoggingService
    {
        public LoggingService()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        public void LogInformation(string message)
        {
            Log.Information(message);
        }

        public void LogError(string message, Exception ex)
        {
            Log.Error(ex, message);
        }
    }
}
