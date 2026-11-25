using AxData.Application.Ports.Axpert;
using AxData.Integration.Axpert.StructDef;
using AxData.TestSupport.Fixtures;
using AxStructDef.Models;

namespace AxData.ContractTests;

public sealed class StructDefProviderContractTests
{
    [Fact]
    public async Task Current_definition_is_loaded_fresh_without_a_revision_cache()
    {
        var source = new StubStructDefSource(Definition());
        var provider = new AxpertStructDefProvider(source);

        var first = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);
        var second = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(2, source.LoadCalls);
    }

    [Fact]
    public async Task Provider_returns_the_current_definition_without_revision_binding()
    {
        var source = new MutableStructDefSource(Definition());
        var provider = new AxpertStructDefProvider(source);

        var first = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);
        source.Current = Definition() with { RequiresCompatibilityFallback = true };
        var second = await provider.GetAsync(SampleStructDefFixture.TenantId, SampleStructDefFixture.TransId, default);

        Assert.False(first?.RequiresCompatibilityFallback);
        Assert.True(second?.RequiresCompatibilityFallback);
    }

    [Fact]
    public async Task Mismatched_source_identity_is_rejected()
    {
        var source = new MismatchedStructDefSource(Definition(tenantId: "wrong-tenant"));
        var provider = new AxpertStructDefProvider(source);

        var definition = await provider.GetAsync(
            SampleStructDefFixture.TenantId,
            SampleStructDefFixture.TransId,
            default);

        Assert.Null(definition);
    }

    [Fact]
    public void Authoritative_ARM_metadata_projects_direction_kind_scope_and_fallback()
    {
        var metadata = new TDependencyMetadata
        {
            RequiresCompatibilityFallback = true,
            Edges =
            [
                new TDependencyEdge
                {
                    Source = "qty",
                    Target = "amount",
                    Kind = TDependencyKind.Expression,
                    DeclarationOrder = 2,
                    SourceFrameNo = 2,
                    TargetFrameNo = 2,
                    SourceIsGrid = true,
                    TargetIsGrid = true,
                    IsResolved = true
                },
                new TDependencyEdge
                {
                    Source = "customer",
                    Target = "missing_target",
                    Kind = TDependencyKind.Select,
                    DeclarationOrder = 3,
                    SourceFrameNo = 1,
                    TargetFrameNo = null,
                    TargetIsPopup = true,
                    IsResolved = false
                }
            ],
            Diagnostics =
            [
                new TDependencyDiagnostic
                {
                    Kind = TDependencyDiagnosticKind.UnresolvedReference,
                    Source = "customer",
                    Target = "missing_target",
                    Message = "The target is unresolved."
                },
                new TDependencyDiagnostic
                {
                    Kind = TDependencyDiagnosticKind.Cycle,
                    Source = "amount",
                    Target = "qty",
                    Message = "A dependency cycle was detected."
                }
            ]
        };

        var projection = AxpertStructDefSource.ProjectDependencyMetadata(metadata);

        Assert.True(projection.RequiresCompatibilityFallback);
        Assert.Collection(
            projection.Dependencies,
            dependency =>
            {
                Assert.Equal("dc2", dependency.SourceContainer);
                Assert.Equal("qty", dependency.SourceField);
                Assert.Equal("dc2", dependency.TargetContainer);
                Assert.Equal("amount", dependency.TargetField);
                Assert.Equal(FieldDependencyKind.Expression, dependency.Kind);
                Assert.Equal(2, dependency.DeclarationOrder);
                Assert.Equal(2, dependency.SourceFrameNo);
                Assert.Equal(2, dependency.TargetFrameNo);
                Assert.True(dependency.SourceIsGrid);
                Assert.True(dependency.TargetIsGrid);
                Assert.True(dependency.IsResolved);
            },
            dependency =>
            {
                Assert.Equal("dc1", dependency.SourceContainer);
                Assert.Null(dependency.TargetContainer);
                Assert.Equal(FieldDependencyKind.Select, dependency.Kind);
                Assert.True(dependency.TargetIsPopup);
                Assert.False(dependency.IsResolved);
            });
        Assert.Collection(
            projection.Diagnostics,
            diagnostic => Assert.Equal(
                FieldDependencyDiagnosticKind.UnresolvedReference,
                diagnostic.Kind),
            diagnostic => Assert.Equal(FieldDependencyDiagnosticKind.Cycle, diagnostic.Kind));
    }

    [Fact]
    public void Empty_authoritative_metadata_does_not_invent_expression_dependencies()
    {
        var projection = AxpertStructDefSource.ProjectDependencyMetadata(new TDependencyMetadata());

        Assert.Empty(projection.Dependencies);
        Assert.Empty(projection.Diagnostics);
        Assert.False(projection.RequiresCompatibilityFallback);
    }

    [Fact]
    public void Evaluation_order_uses_resolved_ordinary_edges_not_display_order_or_special_edges()
    {
        FieldDependency[] dependencies =
        [
            new(
                "dc2",
                "amount",
                "dc2",
                "rate",
                FieldDependencyKind.Label,
                DeclarationOrder: 1,
                SourceFrameNo: 2,
                TargetFrameNo: 2,
                SourceIsGrid: true,
                TargetIsGrid: true),
            new(
                "dc2",
                "qty",
                "dc2",
                "amount",
                FieldDependencyKind.Expression,
                DeclarationOrder: 2,
                SourceFrameNo: 2,
                TargetFrameNo: 2,
                SourceIsGrid: true,
                TargetIsGrid: true)
        ];

        var order = AxpertStructDefSource.DeriveEvaluationOrder(
            2,
            ["amount", "rate", "qty"],
            dependencies);

        Assert.Equal(["qty", "amount", "rate"], order);
    }

    [Fact]
    public async Task Provider_preserves_dependency_fallback_and_diagnostics()
    {
        var diagnostic = new FieldDependencyDiagnostic(
            FieldDependencyDiagnosticKind.Cycle,
            "qty",
            "amount",
            "A dependency cycle was detected.");
        var scriptedDefinition = Definition() with
        {
            RequiresCompatibilityFallback = true,
            DependencyDiagnostics = [diagnostic]
        };
        var provider = new AxpertStructDefProvider(new StubStructDefSource(scriptedDefinition));

        var definition = await provider.GetAsync(
            SampleStructDefFixture.TenantId,
            SampleStructDefFixture.TransId,
            default);

        Assert.NotNull(definition);
        Assert.True(definition.RequiresCompatibilityFallback);
        Assert.Equal(diagnostic, Assert.Single(definition.DependencyDiagnostics!));
    }

    private static StructDefHandle Definition(
        string tenantId = SampleStructDefFixture.TenantId,
        string transId = SampleStructDefFixture.TransId) => new(
            tenantId,
            transId,
            SampleStructDefFixture.Definition.DataContainers,
            SampleStructDefFixture.Definition.Dependencies,
            SampleStructDefFixture.Definition.FillGrids);

    private sealed class StubStructDefSource(params StructDefHandle[] definitions) : IAxpertStructDefSource
    {
        public int LoadCalls { get; private set; }

        public Task<StructDefHandle?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadCalls++;
            var definition = definitions.LastOrDefault(item =>
                Same(item.TenantId, tenantId) && Same(item.TransId, transId));
            return Task.FromResult(definition);
        }
    }

    private sealed class MismatchedStructDefSource(StructDefHandle definition) : IAxpertStructDefSource
    {
        public Task<StructDefHandle?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken) => Task.FromResult<StructDefHandle?>(definition);
    }

    private sealed class MutableStructDefSource(StructDefHandle current) : IAxpertStructDefSource
    {
        public StructDefHandle Current { get; set; } = current;

        public Task<StructDefHandle?> LoadCurrentAsync(
            string tenantId,
            string transId,
            CancellationToken cancellationToken) => Task.FromResult<StructDefHandle?>(Current);
    }

    private static bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
