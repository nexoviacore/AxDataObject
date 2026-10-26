using AxData.Integration.Axpert.FieldEngine;

namespace AxData.Integration.Axpert.Runtime;

public sealed class ProductionAxpertRuntimeContextFactory(
    IAxpertSessionRuntimeAccessor sessionAccessor) : IAxpertRuntimeContextFactory
{
    public async Task<AxpertOperationResult<AxpertRuntimeContext>> CreateAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = sessionAccessor.Current;
        if (current is null ||
            !string.Equals(current.Session.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("The authenticated Axpert session does not match the requested tenant.");
        }

        try
        {
            if (current.Common._dbHelper is null)
            {
                var error = await current.Common.OpenDBConnectionAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return Failure("The Axpert database runtime could not be initialized.");
                }
            }

            var schema = current.Common.GetSchemaName().Split('~', 2)[0];
            if (!string.Equals(schema, tenantId, StringComparison.OrdinalIgnoreCase))
            {
                return Failure("The Axpert database schema does not match the authenticated tenant.");
            }

            return new AxpertOperationResult<AxpertRuntimeContext>(
                true,
                new AxpertRuntimeContext(
                    current.Common,
                    current.TraceService,
                    current.Session.Roles.ToArray(),
                    current.Session.SessionId),
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Failure("The Axpert database runtime could not be initialized.");
        }
    }

    private static AxpertOperationResult<AxpertRuntimeContext> Failure(string error) =>
        new(false, null!, error);
}
