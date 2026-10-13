using AxData.Application.Ports;

namespace AxData.Api.Observability;

public sealed record OperationalLoggingOptions(OperationalLogDetail DetailLevel)
{
    public static OperationalLoggingOptions FromConfiguration(IConfiguration configuration)
    {
        var value = configuration["AxData:Logging:DetailLevel"];
        return new OperationalLoggingOptions(
            Enum.TryParse<OperationalLogDetail>(value, ignoreCase: true, out var detail)
                ? detail
                : OperationalLogDetail.Baseline);
    }
}
