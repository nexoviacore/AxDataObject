using AxData.Application.Init;
using AxData.Application.Ports.Axpert;

namespace AxData.Api.Contracts;

public sealed record InitializeAxDataRequest(string TransId, InitializeAxDataKeyRequest? Key);

public sealed record InitializeAxDataKeyRequest(string? Field, string? Value, long? RecordId = null);

public sealed record InitializeAxDataApiResponse(
    string Status,
    string ObjectId,
    long Version,
    string TransId,
    long? RecordId,
    bool ReadOnly,
    DateTimeOffset ExpiresAt,
    string CorrelationId)
{
    public static InitializeAxDataApiResponse From(
        InitializeAxDataResponse response,
        string correlationId) =>
        new(
            "success",
            response.ObjectId,
            response.Version,
            response.TransId,
            response.RecordId,
            response.ReadOnly,
            response.ExpiresAt,
            correlationId);
}

public static class InitializeAxDataRequestMapping
{
    public static InitializeAxDataCommand ToCommand(this InitializeAxDataRequest request) =>
        new(
            request.TransId,
            request.Key is null
                ? null
                : new AxRecordKey(request.Key.Field, request.Key.Value, request.Key.RecordId));
}
