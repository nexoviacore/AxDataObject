using System.IO.Compression;
using System.Text;
using AxData.Integration.Axpert.FillGrid;

namespace AxData.ContractTests;

public sealed class AxpertFillGridSqlSourceContractTests
{
    [Fact]
    public async Task Parameterless_fillgrid_sql_is_recovered_from_chunked_structdef_xml()
    {
        const string sql = "select nickname, empid from axusers where nickname is not null limit 10";
        var payload = Compress($"""
            <root>
              <struct cat="struct" />
              <batchone cat="fillgrid"><a5>{sql}</a5></batchone>
              <batchtwo cat="fillgrid"><a5><line>{sql}</line></a5></batchtwo>
              <batchthree cat="fillgrid"><a5>{sql}</a5></batchthree>
            </root>
            """);

        var result = await AxpertFillGridSqlSource.ParseAsync(
            [payload[..9], payload[9..]],
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(sql, result.Value["batchone"]);
        Assert.Equal(sql, result.Value["batchtwo"]);
        Assert.Equal(sql, result.Value["batchthree"]);
    }

    [Fact]
    public async Task Duplicate_fillgrid_names_fail_closed()
    {
        var payload = Compress("""
            <root>
              <batchone cat="fillgrid"><a5>select 1</a5></batchone>
              <batchone cat="fillgrid"><a5>select 2</a5></batchone>
            </root>
            """);

        var result = await AxpertFillGridSqlSource.ParseAsync([payload], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Missing_fillgrid_sql_fails_closed()
    {
        var payload = Compress("""
            <root>
              <batchone cat="fillgrid"><a5 /></batchone>
            </root>
            """);

        var result = await AxpertFillGridSqlSource.ParseAsync([payload], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    private static byte[] Compress(string xml)
    {
        var source = Encoding.UTF8.GetBytes(xml);
        using var result = new MemoryStream();
        result.Write(BitConverter.GetBytes((uint)source.Length));
        using (var zlib = new ZLibStream(result, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(source);
        }

        return result.ToArray();
    }
}
