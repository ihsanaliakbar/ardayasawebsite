namespace Ardayasa.Application.Common;

/// <summary>
/// Machine-readable error. <see cref="Code"/> is a stable identifier the frontend
/// maps to an Indonesian message via translation files (e.g. "auth.invalid_credentials");
/// <see cref="Description"/> is an English developer-facing detail, never shown to users.
/// </summary>
public record Error(string Code, string? Description = null);

public class Result
{
    protected Result(bool succeeded, IReadOnlyList<Error> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(params Error[] errors) => new(false, errors);
}

public class Result<T> : Result
{
    private Result(bool succeeded, T? value, IReadOnlyList<Error> errors)
        : base(succeeded, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, []);

    public static new Result<T> Failure(params Error[] errors) => new(false, default, errors);
}
