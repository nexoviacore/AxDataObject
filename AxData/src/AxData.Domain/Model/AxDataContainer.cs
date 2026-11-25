using AxData.Domain.Lifecycle;

namespace AxData.Domain.Model;

public sealed record AxDataFieldMetadata(string Name, bool IsReadOnly = false, bool IsSourceKey = false);

public sealed class AxDataContainer
{
    private readonly List<AxDataFieldMetadata> _fields;

    public AxDataContainer(
        string name,
        bool isGrid,
        IEnumerable<AxDataRow>? rows = null,
        IEnumerable<AxDataFieldMetadata>? fields = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        IsGrid = isGrid;
        Rows = rows?.ToList() ?? [];
        _fields = fields?.ToList() ?? InferFields(Rows.FirstOrDefault());

        if (!isGrid && Rows.Count > 1)
        {
            throw new ArgumentException("A non-grid data container can contain only one logical row.", nameof(rows));
        }

        EnsureValidFields(_fields);
        EnsureUniqueRowIds(Rows);
        foreach (AxDataRow row in Rows)
        {
            EnsureRowMatchesFields(row);
        }
    }

    public string Name { get; }

    public bool IsGrid { get; }

    public IReadOnlyList<AxDataFieldMetadata> Fields => _fields;

    public List<AxDataRow> Rows { get; }

    /// <summary>Adds a row after enforcing row-ID uniqueness and the container's canonical field shape.</summary>
    public void AddRow(AxDataRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!IsGrid && Rows.Count != 0)
        {
            throw new InvalidOperationException("A non-grid data container can contain only one logical row.");
        }

        if (Rows.Any(existing => string.Equals(existing.RowId, row.RowId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Row '{row.RowId}' already exists.");
        }

        if (_fields.Count == 0 && row.Fields.Count > 0)
        {
            _fields.AddRange(InferFields(row));
        }
        EnsureRowMatchesFields(row);
        Rows.Add(row);
    }

    /// <summary>Applies a valid lifecycle transition to the identified row.</summary>
    /// <returns><see langword="true"/> when the row changed; otherwise <see langword="false"/>.</returns>
    public bool ApplyRowState(string rowId, AxRowState target)
    {
        var row = Rows.SingleOrDefault(candidate => string.Equals(candidate.RowId, rowId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Row '{rowId}' was not found.");

        if (row.State == AxRowState.New && target == AxRowState.Deleted)
        {
            Rows.Remove(row);
            return true;
        }

        return row.TransitionTo(target);
    }

    private static void EnsureUniqueRowIds(IEnumerable<AxDataRow> rows)
    {
        var duplicate = rows
            .GroupBy(row => row.RowId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Row '{duplicate.Key}' occurs more than once.", nameof(rows));
        }
    }

    private static List<AxDataFieldMetadata> InferFields(AxDataRow? row) =>
        row?.Fields
            .OrderBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(field => field.Key, StringComparer.Ordinal)
            .Select(field => new AxDataFieldMetadata(
                field.Key,
                field.Value.IsReadOnly,
                field.Value.IsSourceKey))
            .ToList() ?? [];

    private static void EnsureValidFields(IReadOnlyList<AxDataFieldMetadata> fields)
    {
        if (fields.Any(field => string.IsNullOrWhiteSpace(field.Name)) ||
            fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != fields.Count)
        {
            throw new ArgumentException("Field metadata contains a blank or duplicate field name.", nameof(fields));
        }
    }

    private void EnsureRowMatchesFields(AxDataRow row)
    {
        if (row.Fields.Count != _fields.Count ||
            _fields.Any(field => !row.Fields.TryGetValue(field.Name, out AxFieldValue? value) ||
                                 value.IsReadOnly != field.IsReadOnly ||
                                 value.IsSourceKey != field.IsSourceKey))
        {
            throw new ArgumentException(
                $"Row '{row.RowId}' does not match the field metadata for data container '{Name}'.",
                nameof(row));
        }
    }
}
