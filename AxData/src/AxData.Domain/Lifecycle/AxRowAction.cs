namespace AxData.Domain.Lifecycle;

public static class AxRowAction
{
    public const string New = "n";
    public const string Edit = "e";
    public const string Delete = "d";

    public static string ToCode(AxRowState state) => state switch
    {
        AxRowState.New => New,
        AxRowState.Edited => Edit,
        AxRowState.Deleted => Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unchanged rows have no action code.")
    };

    public static AxRowState FromCode(string code) => code switch
    {
        New => AxRowState.New,
        Edit => AxRowState.Edited,
        Delete => AxRowState.Deleted,
        _ => throw new ArgumentException("Row action must be 'n', 'e', or 'd'.", nameof(code))
    };
}
