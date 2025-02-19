using Hearth.ArcGIS.Samples.PlugIns.Contracts;
using Hearth.ArcGIS.Samples.Services;
using Microsoft.Extensions.Logging;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class LogSampleButton : InjectableButton
    {
        [Inject]
        private readonly ILogger? _logger;
        [Inject]
        private readonly ILogger<LogSampleButton>? _typeLogger;
        [Inject]
        private readonly TestLogService? _testLogService;


        protected override void OnClick()
        {
            _logger?.LogTrace("Default Logger LogTrace");
            _logger?.LogDebug("Default Logger LogDebug");
            _logger?.LogInformation("Default Logger LogInformation");
            _logger?.LogWarning("Default Logger LogWarning");
            _logger?.LogError("Default Logger LogError");
            _logger?.LogCritical("Default Logger LogCritical");

            _typeLogger?.LogTrace("Not configured Type Logger Class LogTrace");
            _typeLogger?.LogDebug("Not configured Type Logger Class LogDebug");
            _typeLogger?.LogInformation("Not configured Type Logger Class LogInformation");
            _typeLogger?.LogWarning("Not configured Type Logger Class LogWarning");
            _typeLogger?.LogError("Not configured Type Logger Class LogError");
            _typeLogger?.LogCritical("Not configured Type Logger Class LogCritical");

            _testLogService?.WriteLog();
        }
    }
}
