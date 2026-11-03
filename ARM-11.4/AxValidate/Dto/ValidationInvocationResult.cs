namespace AxValidate.Dto;

public enum ValidationResultCategory
{
    Submission,
    Dependency,
    FinalValidation
}

public enum ValidationResultSeverity
{
    Processed,
    Warning,
    Error
}

public sealed class ValidationResultEntry
{
    public ValidationAddress Address { get; init; } = new();
    public ValidationResultCategory Category { get; init; }
    public ValidationResultSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class ValidationInvocationResult
{
    public bool Success => Errors.Count == 0;
    public string LegacyMessage { get; set; } = string.Empty;
    public List<ValidationResultEntry> ProcessedFields { get; } = new();
    public List<ValidationResultEntry> Warnings { get; } = new();
    public List<ValidationResultEntry> Errors { get; } = new();

    public void Add(ValidationResultEntry entry)
    {
        switch (entry.Severity)
        {
            case ValidationResultSeverity.Processed:
                ProcessedFields.Add(entry);
                break;
            case ValidationResultSeverity.Warning:
                Warnings.Add(entry);
                break;
            case ValidationResultSeverity.Error:
                Errors.Add(entry);
                break;
        }
    }
}
