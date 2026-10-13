using System.Text.Json;
using System.Text.Json.Serialization;
using AxData.Application.Ports;

namespace AxData.Workers.Observability;

public sealed class ConsoleOperationalEventSink(OperationalLogDetail detailLevel) : IOperationalEventSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Write(OperationalEvent operationalEvent)
    {
        ArgumentNullException.ThrowIfNull(operationalEvent);
        if (operationalEvent.Detail <= detailLevel)
        {
            Console.WriteLine(JsonSerializer.Serialize(operationalEvent, JsonOptions));
        }
    }
}
