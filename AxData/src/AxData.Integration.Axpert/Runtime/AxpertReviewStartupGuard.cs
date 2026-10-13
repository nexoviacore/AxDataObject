using System.Data;
using ARMCommon.Helpers;

namespace AxData.Integration.Axpert.Runtime;

public interface IAxpertReviewStartupGuard
{
    Task<bool> IsReadOnlyAsync(CancellationToken cancellationToken);
}

public sealed class AxpertReviewStartupGuard(AxpertReviewRuntimeOptions options)
    : IAxpertReviewStartupGuard
{
    private const string ReadOnlyAuditSql = """
        SELECT NOT (
            has_database_privilege(current_user, current_database(), 'CREATE')
            OR has_schema_privilege(current_user, @schema, 'CREATE')
            OR EXISTS (
                SELECT 1
                FROM pg_class AS object
                INNER JOIN pg_namespace AS namespace ON namespace.oid = object.relnamespace
                WHERE namespace.nspname = @schema
                  AND object.relkind IN ('r', 'p', 'v', 'm', 'f')
                  AND (
                      has_table_privilege(current_user, object.oid, 'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
                      OR has_any_column_privilege(current_user, object.oid, 'INSERT,UPDATE,REFERENCES')
                  )
            )
            OR EXISTS (
                SELECT 1
                FROM pg_class AS object
                INNER JOIN pg_namespace AS namespace ON namespace.oid = object.relnamespace
                WHERE namespace.nspname = @schema
                  AND object.relkind = 'S'
                  AND has_sequence_privilege(current_user, object.oid, 'USAGE,UPDATE')
            )
        )
        """;

    public async Task<bool> IsReadOnlyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(options.DatabaseType, "postgre", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var helper = DBHelper.CreateDbHelper(options.DatabaseType);
        helper.ConnectionString = options.ConnectionString;
        try
        {
            await helper.OpenDBConnectionAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var result = await helper.ExecuteScalarAsync(
                ReadOnlyAuditSql,
                ["@schema"],
                [DbType.String],
                [options.TenantId]).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return string.IsNullOrWhiteSpace(result.error) &&
                   result.result is not null &&
                   Convert.ToBoolean(result.result, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
        finally
        {
            await helper.CloseDBConnectionAsync().ConfigureAwait(false);
        }
    }
}
