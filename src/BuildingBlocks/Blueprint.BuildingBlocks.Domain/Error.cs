namespace Blueprint.BuildingBlocks.Domain;

/// <summary>Fachlicher Fehler. Wird an der API-Grenze auf RFC 9457 Problem Details abgebildet.</summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
}
