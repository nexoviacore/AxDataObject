using AxData.Application.Ports;

namespace AxData.TestSupport.Fakes;

public sealed class RecordingOperationalEventSink : IOperationalEventSink
{
    public List<OperationalEvent> Events { get; } = [];

    public void Write(OperationalEvent operationalEvent)
    {
        ArgumentNullException.ThrowIfNull(operationalEvent);
        Events.Add(operationalEvent);
    }
}
