using AxData.Domain.Lifecycle;

namespace AxData.Domain.Model;

public sealed class AxDataContainer
{
    public AxDataContainer(string name, bool isGrid, IEnumerable<AxDataRow>? rows = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        IsGrid = isGrid;
        Rows = rows?.ToList() ?? [];

        if (!isGrid && Rows.Count > 1)
        {
            throw new ArgumentException("A non-grid data container can contain only one logical row.", nameof(rows));
        }

        EnsureUniqueRowIds(Rows);
    }

    public string Name { get; }

    public bool IsGrid { get; }

    public List<AxDataRow> Rows { get; }

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

        Rows.Add(row);
    }

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
}
