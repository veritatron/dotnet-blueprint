using System.Diagnostics.CodeAnalysis;

namespace Blueprint.BuildingBlocks.Domain;

/// <summary>
/// Ergebnis einer fachlichen Operation. Erwartbare Fehler werden als Wert zurückgegeben statt als Exception.
/// </summary>
/// <remarks>
/// Exceptions nur für Ausnahmefälle verwenden:
/// https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions#design-classes-so-that-exceptions-can-be-avoided
/// </remarks>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Ungültige Kombination aus Erfolg und Fehler.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Auf den Wert eines fehlgeschlagenen Results kann nicht zugegriffen werden.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
