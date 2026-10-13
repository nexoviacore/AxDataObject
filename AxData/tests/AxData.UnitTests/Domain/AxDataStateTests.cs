using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.UnitTests.Domain;

public sealed class AxDataStateTests
{
    [Fact]
    public void New_state_starts_active_at_version_one()
    {
        var state = CreateState();

        Assert.Equal(AxDataLifecycleStatus.Active, state.Status);
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void Valid_transition_changes_status_and_increments_version_once()
    {
        var state = CreateState();

        var changed = state.TransitionTo(AxDataLifecycleStatus.PendingApproval);

        Assert.True(changed);
        Assert.Equal(AxDataLifecycleStatus.PendingApproval, state.Status);
        Assert.Equal(2, state.Version);
    }

    [Fact]
    public void Repeating_current_status_is_a_no_op()
    {
        var state = CreateState();

        var changed = state.TransitionTo(AxDataLifecycleStatus.Active);

        Assert.False(changed);
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void Invalid_transition_does_not_change_status_or_version()
    {
        var state = CreateState();

        Assert.Throws<InvalidOperationException>(() => state.TransitionTo(AxDataLifecycleStatus.ApprovedAwaitingSave));
        Assert.Equal(AxDataLifecycleStatus.Active, state.Status);
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void Closed_state_is_terminal_and_cannot_register_mutations()
    {
        var state = CreateState();
        state.TransitionTo(AxDataLifecycleStatus.Closed);
        var version = state.Version;

        Assert.Throws<InvalidOperationException>(() => state.TransitionTo(AxDataLifecycleStatus.Active));
        Assert.Throws<InvalidOperationException>(() => state.RegisterMutation());
        Assert.Equal(version, state.Version);
    }

    [Fact]
    public void Registering_a_successful_mutation_increments_version_once()
    {
        var state = CreateState();

        var version = state.RegisterMutation();

        Assert.Equal(2, version);
        Assert.Equal(2, state.Version);
    }

    private static AxDataState CreateState() => new(
        "01JOBJECT",
        "tenant",
        "trans",
        "owner",
        "initiator",
        "struct-v1",
        DateTimeOffset.Parse("2026-09-25T00:00:00Z"));
}
