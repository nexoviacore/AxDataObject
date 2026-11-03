namespace AxValidate.Dto;

public sealed class ValidationAddress
{
    public string DataContainer { get; init; } = string.Empty;
    public int FrameNo { get; init; }
    public int RowNo { get; init; }
    public string FieldName { get; init; } = string.Empty;
}
