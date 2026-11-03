namespace AxValidate.Dto;

public class OperationResult
{
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationInvocationResult Details { get; set; } = new();
}
