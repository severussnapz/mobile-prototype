using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace Genesis.AI.Core.Logging;

public sealed class DynatraceTextFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var logEntry = new Dictionary<string, object?>
        {
            ["timestamp"] = logEvent.Timestamp.UtcDateTime.ToString("O"),
            ["level"] = logEvent.Level.ToString(),
            ["message"] = logEvent.RenderMessage(System.Globalization.CultureInfo.InvariantCulture)
        };

        foreach (var property in logEvent.Properties)
        {
            logEntry[property.Key] = property.Value.ToString();
        }

        if (logEvent.Exception is not null)
        {
            logEntry["exception"] = logEvent.Exception.ToString();
        }

        output.WriteLine(JsonSerializer.Serialize(logEntry));
    }
}
