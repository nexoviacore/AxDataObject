using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.UnitTests.Domain;

public sealed class AxDataStateTests
{
    [Fact]
    public void New_state_starts_active()
    {
        var state = CreateState();

        Assert.Equal(AxDataLifecycleStatus.Active, state.Status);
    }

    [Fact]
    public void Valid_transition_changes_status()
    {
        var state = CreateState();

        var changed = state.TransitionTo(AxDataLifecycleStatus.PendingApproval);

        Assert.True(changed);
        Assert.Equal(AxDataLifecycleStatus.PendingApproval, state.Status);
    }

    [Fact]
    public void Repeating_current_status_is_a_no_op()
    {
        var state = CreateState();

        var changed = state.TransitionTo(AxDataLifecycleStatus.Active);

        Assert.False(changed);
    }

    [Fact]
    public void Invalid_transition_does_not_change_status()
    {
        var state = CreateState();

        Assert.Throws<InvalidOperationException>(() => state.TransitionTo(AxDataLifecycleStatus.ApprovedAwaitingSave));
        Assert.Equal(AxDataLifecycleStatus.Active, state.Status);
    }

    [Fact]
    public void Closed_state_is_terminal_and_cannot_register_mutations()
    {
        var state = CreateState();
        state.TransitionTo(AxDataLifecycleStatus.Closed);
        Assert.Throws<InvalidOperationException>(() => state.TransitionTo(AxDataLifecycleStatus.Active));
        Assert.Throws<InvalidOperationException>(() => state.RegisterMutation());
    }

    [Fact]
    public void Active_state_can_register_a_mutation()
    {
        var state = CreateState();

        state.RegisterMutation();
    }

    [Fact]
    public void Save_claim_is_internal_and_retained_finalization_returns_to_active()
    {
        var state = CreateState();

        state.BeginSaving();

        Assert.Equal(AxDataLifecycleStatus.Saving, state.Status);
        Assert.Throws<InvalidOperationException>(() => state.RegisterMutation());

        state.FinalizeSaving(retainDataObject: true);

        Assert.Equal(AxDataLifecycleStatus.Active, state.Status);
        state.RegisterMutation();
    }

    [Fact]
    public void Save_claim_rollback_restores_active_state()
    {
        var state = CreateState();
        state.BeginSaving();

        state.RollbackSaving();

        Assert.Equal(AxDataLifecycleStatus.Active, state.Status);
    }

    private static AxDataState CreateState() => new(
        "01JOBJECT",
        "tenant",
        "trans",
        "initiator",
        DateTimeOffset.Parse("2026-09-25T00:00:00Z"));
}
