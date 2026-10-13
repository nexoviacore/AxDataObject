namespace AxData.Domain.Model;

public sealed record AxFileReference(string FileId, string FileName, string? ContentType = null);
