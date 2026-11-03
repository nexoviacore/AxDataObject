namespace AxStructDef.Models
{
    public enum TDependencyKind
    {
        Expression,
        Select,
        Fill,
        Aggregate,
        FillGrid,
        DataContainer,
        Label,
        DependentFrame,
        Grid
    }

    public enum TDependencyDiagnosticKind
    {
        MalformedMetadata,
        UnresolvedReference,
        Cycle
    }

    public sealed class TDependencyEdge
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public TDependencyKind Kind { get; set; }
        public int DeclarationOrder { get; set; }
        public int SourceFrameNo { get; set; }
        public int? TargetFrameNo { get; set; }
        public bool SourceIsGrid { get; set; }
        public bool TargetIsGrid { get; set; }
        public bool TargetIsPopup { get; set; }
        public bool IsResolved { get; set; }
    }

    public sealed class TDependencyDiagnostic
    {
        public TDependencyDiagnosticKind Kind { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class TDependencyMetadata
    {
        public List<TDependencyEdge> Edges { get; set; } = new();
        public List<TDependencyDiagnostic> Diagnostics { get; set; } = new();
        public bool RequiresCompatibilityFallback { get; set; }

        public IReadOnlyList<TDependencyEdge> GetOutgoing(string source)
        {
            return Edges
                .Where(edge => string.Equals(edge.Source, source, StringComparison.OrdinalIgnoreCase))
                .OrderBy(edge => edge.DeclarationOrder)
                .ToList();
        }

        public IReadOnlyList<TDependencyEdge> GetParents(string target)
        {
            return Edges
                .Where(edge => string.Equals(edge.Target, target, StringComparison.OrdinalIgnoreCase))
                .OrderBy(edge => edge.DeclarationOrder)
                .ToList();
        }
    }
}
