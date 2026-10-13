namespace AxData.Domain.Errors;

public static class AxDataErrors
{
    public static readonly AxDataErrorDefinition AuthenticationRequired = new("AXD-AUTH-001", "authentication", "Authentication is required.", 401);
    public static readonly AxDataErrorDefinition AccessDenied = new("AXD-AUTH-002", "authorization", "Access is denied.", 403);
    public static readonly AxDataErrorDefinition ObjectNotFound = new("AXD-NOTFOUND-001", "not-found", "The AxData object was not found.", 404);
    public static readonly AxDataErrorDefinition ObjectExpired = new("AXD-NOTFOUND-002", "not-found", "The AxData object has expired.", 410);
    public static readonly AxDataErrorDefinition InvalidRequest = new("AXD-REQ-001", "request", "The request is invalid.", 400);
    public static readonly AxDataErrorDefinition ValidationFailed = new("AXD-VAL-001", "validation", "Validation failed.", 422);
    public static readonly AxDataErrorDefinition StaleVersion = new("AXD-CONC-001", "concurrency", "The AxData object was changed by another request.", 409);
    public static readonly AxDataErrorDefinition ActiveConflict = new("AXD-CONC-002", "concurrency", "The AxData object has an active conflicting operation.", 409);
    public static readonly AxDataErrorDefinition StructDefChanged = new("AXD-CONC-003", "concurrency", "The transaction definition changed. Re-initialize the AxData object.", 409);
    public static readonly AxDataErrorDefinition IdempotencyConflict = new("AXD-IDEMP-001", "idempotency", "The idempotency key conflicts with an earlier request.", 409);
    public static readonly AxDataErrorDefinition InvalidWorkflowState = new("AXD-PEG-001", "workflow", "The workflow action is not valid in the current state.", 409);
    public static readonly AxDataErrorDefinition InvalidWorkflowAction = new("AXD-PEG-002", "workflow", "The workflow action is invalid.", 422);
    public static readonly AxDataErrorDefinition SaveValidationFailed = new("AXD-SAVE-001", "save", "The object cannot be saved because validation failed.", 422);
    public static readonly AxDataErrorDefinition SaveFailed = new("AXD-SAVE-002", "save", "The object could not be saved.", 500);
    public static readonly AxDataErrorDefinition DependencyUnavailable = new("AXD-INFRA-001", "infrastructure", "A required service is unavailable.", 503);
    public static readonly AxDataErrorDefinition RecoveryFailed = new("AXD-INFRA-002", "infrastructure", "The AxData object could not be recovered.", 500);

    public static IReadOnlyList<AxDataErrorDefinition> All { get; } =
    [
        AuthenticationRequired,
        AccessDenied,
        ObjectNotFound,
        ObjectExpired,
        InvalidRequest,
        ValidationFailed,
        StaleVersion,
        ActiveConflict,
        StructDefChanged,
        IdempotencyConflict,
        InvalidWorkflowState,
        InvalidWorkflowAction,
        SaveValidationFailed,
        SaveFailed,
        DependencyUnavailable,
        RecoveryFailed
    ];
}
