using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.StructDef;
using AxData.TestSupport.Fixtures;
using ARMCommon.Model;
using System.Data;
using System.Security.Cryptography;

namespace AxData.ContractTests;

public sealed class StructDefProviderContractTests
{
    [Fact]
    public async Task Current_definition_uses_content_hash_and_hash_keyed_cache()
    {
        var source = new StubStructDefSource(Snapshot(Hash('a')));
        var provider = new AxpertStructDefProvider(source);

        var first = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);
        var second = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);

        Assert.NotNull(first);
        Assert.Equal(Hash('a'), first.Version);
        Assert.Same(first, second);
        Assert.Equal(2, source.LoadCalls);
    }

    [Fact]
    public async Task Current_hash_change_is_detected_and_projects_a_new_definition()
    {
        var source = new MutableStructDefSource(Snapshot(Hash('a')));
        var provider = new AxpertStructDefProvider(source);

        var first = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);
        source.Current = Snapshot(Hash('b'));
        var second = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);
        var stale = await provider.GetVersionAsync(
            SampleStructDefFixture.TenantId,
            SampleStructDefFixture.TransId,
            Hash('a'),
            default);

        Assert.Equal(Hash('a'), first?.Version);
        Assert.Equal(Hash('b'), second?.Version);
        Assert.Null(stale);
    }

    [Fact]
    public async Task Expected_hash_fails_closed_when_current_row_changed()
    {
        var provider = new AxpertStructDefProvider(new StubStructDefSource(Snapshot(Hash('c'))));

        var missing = await provider.GetVersionAsync(
            SampleStructDefFixture.TenantId,
            SampleStructDefFixture.TransId,
            Hash('d'),
            default);

        Assert.Null(missing);
    }

    [Fact]
    public async Task Mismatched_source_identity_is_rejected()
    {
        var source = new MismatchedStructDefSource(Snapshot(Hash('e'), tenantId: "wrong-tenant"));
        var provider = new AxpertStructDefProvider(source);

        var definition = await provider.GetVersionAsync(
            SampleStructDefFixture.TenantId,
            SampleStructDefFixture.TransId,
            Hash('e'),
            default);

        Assert.Null(definition);
    }

    [Fact]
    public void Content_hash_covers_raw_props_chunks_in_blob_order()
    {
        var result = new SQLResult();
        result.data.Columns.Add("props", typeof(byte[]));
        result.data.Rows.Add(new byte[] { 1, 2, 3 });
        result.data.Rows.Add(new byte[] { 4, 5 });

        var actual = AxpertStructDefSource.ComputeContentHash(result);
        var expected = Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3, 4, 5 }))
            .ToLowerInvariant();

        Assert.Equal(expected, actual);
    }

    private static AxpertStructDefSnapshot Snapshot(
        string contentHash,
        string tenantId = SampleStructDefFixture.TenantId,
        string transId = SampleStructDefFixture.TransId) => new(
            tenantId,
            transId,
            contentHash,
            SampleStructDefFixture.Definition.DataContainers,
            SampleStructDefFixture.Definition.Dependencies,
            SampleStructDefFixture.Definition.FillGrids);

    private sealed class StubStructDefSource(params AxpertStructDefSnapshot[] snapshots) : IAxpertStructDefSource
    {
        public int LoadCalls { get; private set; }

        public Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadCalls++;
            var snapshot = snapshots.LastOrDefault(item =>
                Same(item.TenantId, tenantId) && Same(item.TransId, transId));
            return Task.FromResult(snapshot);
        }
    }

    private sealed class MismatchedStructDefSource(AxpertStructDefSnapshot snapshot) : IAxpertStructDefSource
    {
        public Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken) => Task.FromResult<AxpertStructDefSnapshot?>(snapshot);
    }

    private sealed class MutableStructDefSource(AxpertStructDefSnapshot current) : IAxpertStructDefSource
    {
        public AxpertStructDefSnapshot Current { get; set; } = current;

        public Task<AxpertStructDefSnapshot?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken) => Task.FromResult<AxpertStructDefSnapshot?>(Current);
    }

    private static string Hash(char value) => new(value, 64);

    private static bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
