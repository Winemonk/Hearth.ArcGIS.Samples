using Microsoft.Extensions.Logging;

namespace Hearth.ArcGIS.Samples.Services
{
    public class TestLogService : ITransientService
    {
        private readonly ILogger<TestLogService> _logger;
        public TestLogService(ILogger<TestLogService> logger)
        {
            _logger = logger;
        }

        public void WriteLogs()
        {
            _logger?.LogTrace("Configured Type Logger Class LogTrace");
            _logger?.LogDebug("Configured Type Logger Class LogDebug");
            _logger?.LogInformation("Configured Type Logger Class LogInformation");
            _logger?.LogWarning("Configured Type Logger Class LogWarning");
            _logger?.LogError("Configured Type Logger Class LogError");
            _logger?.LogCritical("Configured Type Logger Class LogCritical");
        }

        public void WriteStructuredLogs()
        {
            Entry entry = new Entry
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Time = DateTime.Now
            };
            _logger?.LogInformation("Structured Log - EntryId: {EntryId}, EntryName: {EntryName}, EntryTime: {EntryTime}", entry.Id, entry.Name, entry.Time);
            _logger?.LogInformation("Structured Log - Entry: {@Entry}", entry);
        }
    }

    public class Entry
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime Time { get; set; }
    }
}