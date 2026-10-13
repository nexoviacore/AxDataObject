using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.UnitTests.Domain;

public sealed class AxDataRowTests
{
    [Theory]
    [InlineData(AxRowState.New, "n")]
    [InlineData(AxRowState.Edited, "e")]
    [InlineData(AxRowState.Deleted, "d")]
    public void Row_states_map_to_compatibility_action_codes(AxRowState state, string code)
    {
        Assert.Equal(code, AxRowAction.ToCode(state));
        Assert.Equal(state, AxRowAction.FromCode(code));
    }

    [Fact]
    public void New_row_has_opaque_identity_separate_from_record_id()
    {
        var row = new AxDataRow("row_alpha", AxRowState.New);

        Assert.Equal("row_alpha", row.RowId);
        Assert.Null(row.PersistedRecordId);
        Assert.Throws<ArgumentException>(() => new AxDataRow("row_beta", AxRowState.New, 42));
    }

    [Fact]
    public void Existing_row_requires_record_id_without_constraining_opaque_row_id()
    {
        Assert.Throws<ArgumentException>(() => new AxDataRow("row_alpha", AxRowState.Edited));
        var row = new AxDataRow("row_alpha", AxRowState.Edited, 42);
        Assert.Equal("row_alpha", row.RowId);
    }

    [Fact]
    public void Existing_row_can_be_edited_then_deleted()
    {
        var row = new AxDataRow("row_42", AxRowState.Unchanged, 42);

        Assert.True(row.TransitionTo(AxRowState.Edited));
        Assert.True(row.TransitionTo(AxRowState.Deleted));
        Assert.Equal(AxRowState.Deleted, row.State);
    }

    [Fact]
    public void Deleted_row_cannot_be_edited()
    {
        var row = new AxDataRow("row_42", AxRowState.Deleted, 42);

        Assert.Throws<InvalidOperationException>(() => row.TransitionTo(AxRowState.Edited));
    }

    [Fact]
    public void Deleting_unpersisted_new_row_removes_it_from_container()
    {
        var row = new AxDataRow("row_new", AxRowState.New);
        var container = new AxDataContainer("items", true, [row]);

        var changed = container.ApplyRowState("row_new", AxRowState.Deleted);

        Assert.True(changed);
        Assert.Empty(container.Rows);
    }

    [Fact]
    public void Assigning_record_id_preserves_row_id_and_marks_row_persisted()
    {
        var row = new AxDataRow("row_stable", AxRowState.New);

        row.AssignPersistedRecordId(5001);

        Assert.Equal("row_stable", row.RowId);
        Assert.Equal(5001, row.PersistedRecordId);
        Assert.Equal(AxRowState.Unchanged, row.State);
    }

    [Fact]
    public void Non_grid_container_allows_only_one_logical_row()
    {
        var first = new AxDataRow("row_first", AxRowState.Unchanged, 1);
        var second = new AxDataRow("row_second", AxRowState.Unchanged, 2);

        Assert.Throws<ArgumentException>(() => new AxDataContainer("dc1", false, [first, second]));
    }
}
