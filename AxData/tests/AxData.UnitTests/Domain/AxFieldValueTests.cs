using AxData.Domain.Model;

namespace AxData.UnitTests.Domain;

public sealed class AxFieldValueTests
{
    [Fact]
    public void Changing_and_restoring_value_updates_dirty_state_against_original()
    {
        var field = new AxFieldValue("original");

        Assert.True(field.SetValue("changed"));
        Assert.True(field.IsDirty);
        Assert.Equal("original", field.OriginalValue);

        Assert.True(field.SetValue("original"));
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void Setting_same_value_is_a_no_op()
    {
        var field = new AxFieldValue(10);

        Assert.False(field.SetValue(10));
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void Binary_values_compare_by_content()
    {
        var field = new AxFieldValue(new byte[] { 1, 2, 3 });

        Assert.False(field.SetValue(new byte[] { 1, 2, 3 }));
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void Read_only_field_rejects_changes()
    {
        var field = new AxFieldValue("value", isReadOnly: true);

        Assert.Throws<InvalidOperationException>(() => field.SetValue("other"));
        Assert.Equal("value", field.Value);
    }

    [Fact]
    public void Accepting_changes_sets_a_new_baseline()
    {
        var field = new AxFieldValue("original");
        field.SetValue("changed");

        field.AcceptChanges();

        Assert.Equal("changed", field.OriginalValue);
        Assert.False(field.IsDirty);
    }

    [Fact]
    public void Source_id_participates_in_dirty_tracking_and_acceptance()
    {
        var field = new AxFieldValue("display", sourceId: 10);

        Assert.True(field.SetValue("display", 11));
        Assert.True(field.IsDirty);
        Assert.Equal(10, field.OriginalSourceId);

        field.AcceptChanges();

        Assert.False(field.IsDirty);
        Assert.Equal(11, field.SourceId);
        Assert.Equal(11, field.OriginalSourceId);
    }

    [Fact]
    public void Rehydrate_rejects_inconsistent_or_non_positive_source_state()
    {
        Assert.Throws<ArgumentException>(() =>
            AxFieldValue.Rehydrate("same", "same", true, false, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxFieldValue("value", sourceId: 0));
    }
}
