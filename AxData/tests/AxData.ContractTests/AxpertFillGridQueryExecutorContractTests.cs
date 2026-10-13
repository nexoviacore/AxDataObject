using System.Data;
using System.Reflection;
using ARMCommon.Interface;
using ARMCommon.Model;
using AxData.Integration.Axpert.FillGrid;

namespace AxData.ContractTests;

public sealed class AxpertFillGridQueryExecutorContractTests
{
    [Fact]
    public async Task Parameterized_select_applies_timeout_cap_and_deduplicates_parameters()
    {
        var table = new DataTable();
        table.Columns.Add("paycode", typeof(int));
        table.Rows.Add(42);
        var recorder = RecordingDbHelper.Create(table, out var helper);
        var executor = new AxpertFillGridQueryExecutor(
            helper,
            new AxpertFillGridQueryOptions
            {
                StatementTimeoutMilliseconds = 1_250,
                MaximumRows = 2
            });
        var query = Query(
            "with source as (select :category::int as paycode, ':ignored' as label) " +
            "select paycode from source where paycode = :category",
            ["category", "CATEGORY"]);

        var result = await executor.ExecuteAsync(
            query,
            new Dictionary<string, object?> { ["CATEGORY"] = 42 },
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal("SET statement_timeout = 1250", recorder.TimeoutSql);
        Assert.Contains("@category::int", recorder.QuerySql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT 3", recorder.QuerySql, StringComparison.Ordinal);
        Assert.Equal(["@category"], recorder.ParameterNames);
        Assert.Equal([DbType.Int32], recorder.ParameterTypes);
        Assert.Equal([42], recorder.ParameterValues);
        Assert.Equal(42, Assert.Single(result.Value!)["paycode"]);
    }

    [Theory]
    [InlineData("update paycodes set code = :code", "code")]
    [InlineData("with removed as (delete from paycodes returning code) select code from removed", "")]
    [InlineData("select 1; select 2", "")]
    [InlineData("select {code}", "")]
    [InlineData("select :code", "other")]
    [InlineData("select $1", "code")]
    public void Unsafe_or_unbound_sql_is_rejected(string sql, string declared)
    {
        var names = string.IsNullOrWhiteSpace(declared) ? [] : new[] { declared };

        var result = FillGridSqlPolicy.Prepare(sql, names);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void PostgreSql_quotes_comments_and_dollar_quotes_do_not_create_false_tokens_or_parameters()
    {
        const string sql = "select ':ignored' as text, $$delete; :ignored$$ as body, :code::int as code " +
                           "/* update ignored */ -- insert ignored\n" +
                           "where :code::int > 0";

        var result = FillGridSqlPolicy.Prepare(sql, ["code", "CODE"]);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(["code"], result.Value!.ParameterNames);
        Assert.Contains("@code::int", result.Value.SqlText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Row_cap_overflow_fails_without_returning_partial_rows()
    {
        var table = new DataTable();
        table.Columns.Add("paycode", typeof(string));
        table.Rows.Add("A");
        table.Rows.Add("B");
        table.Rows.Add("C");
        RecordingDbHelper.Create(table, out var helper);
        var executor = new AxpertFillGridQueryExecutor(
            helper,
            new AxpertFillGridQueryOptions { MaximumRows = 2 });

        var result = await executor.ExecuteAsync(
            Query("select paycode from paycodes", []),
            new Dictionary<string, object?>(),
            default);

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Timeout_configuration_failure_stops_before_the_select()
    {
        var recorder = RecordingDbHelper.Create(new DataTable(), out var helper);
        recorder.TimeoutError = "denied";
        var executor = new AxpertFillGridQueryExecutor(helper, new AxpertFillGridQueryOptions());

        var result = await executor.ExecuteAsync(
            Query("select 1", []),
            new Dictionary<string, object?>(),
            default);

        Assert.False(result.Succeeded);
        Assert.Null(recorder.QuerySql);
    }

    [Fact]
    public async Task Failed_database_result_is_not_consumed()
    {
        var table = new DataTable();
        table.Columns.Add("paycode", typeof(string));
        table.Rows.Add("stale");
        var recorder = RecordingDbHelper.Create(table, out var helper);
        recorder.QueryError = "scripted failure";
        var executor = new AxpertFillGridQueryExecutor(helper, new AxpertFillGridQueryOptions());

        var result = await executor.ExecuteAsync(
            Query("select paycode from paycodes", []),
            new Dictionary<string, object?>(),
            default);

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, int.MaxValue)]
    public void Invalid_safety_limits_are_rejected(int timeout, int rows)
    {
        RecordingDbHelper.Create(new DataTable(), out var helper);

        Assert.Throws<InvalidOperationException>(() => new AxpertFillGridQueryExecutor(
            helper,
            new AxpertFillGridQueryOptions
            {
                StatementTimeoutMilliseconds = timeout,
                MaximumRows = rows
            }));
    }

    private static AxpertFillGridQuery Query(string sql, IReadOnlyList<string> parameters) => new(
        "fillgrid1",
        sql,
        "dc1",
        "dc2",
        parameters,
        new Dictionary<string, string> { ["paycode"] = "paycode" },
        3,
        true,
        false);

    // DispatchProxy creates a runtime subclass, so this test double cannot be sealed.
    private class RecordingDbHelper : DispatchProxy
    {
        private DataTable _table = new();

        public string? TimeoutSql { get; private set; }

        public string? QuerySql { get; private set; }

        public IReadOnlyList<string>? ParameterNames { get; private set; }

        public IReadOnlyList<DbType>? ParameterTypes { get; private set; }

        public IReadOnlyList<object>? ParameterValues { get; private set; }

        public string TimeoutError { get; set; } = string.Empty;

        public string QueryError { get; set; } = string.Empty;

        public static RecordingDbHelper Create(DataTable table, out IDbHelper helper)
        {
            helper = DispatchProxy.Create<IDbHelper, RecordingDbHelper>();
            var recorder = (RecordingDbHelper)(object)helper;
            recorder._table = table;
            return recorder;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(IDbHelper.ExecuteNonQueryAsync) &&
                args is [string timeoutSql, null, null, null])
            {
                TimeoutSql = timeoutSql;
                return Task.FromResult(new NonQueryResult
                {
                    error = TimeoutError,
                    message = string.Empty
                });
            }

            if (targetMethod.Name == nameof(IDbHelper.ExecuteSQLAsync) &&
                args is [string querySql, string[] names, DbType[] types, object[] values])
            {
                QuerySql = querySql;
                ParameterNames = names;
                ParameterTypes = types;
                ParameterValues = values;
                return Task.FromResult(new SQLResult
                {
                    error = QueryError,
                    success = string.IsNullOrWhiteSpace(QueryError),
                    count = _table.Rows.Count,
                    data = _table
                });
            }

            throw new NotSupportedException($"The test DB helper does not support '{targetMethod.Name}'.");
        }
    }
}
