namespace PriceHunt.Api.Validation;

/// <summary>
/// Outcome of turning a raw query string into a validated request: either
/// the <typeparamref name="T"/> or the validation errors keyed by parameter
/// name as the client sent it. Created through <see cref="ParseResult"/>.
/// </summary>
public sealed class ParseResult<T>
    where T : class
{
    internal ParseResult(T? value, IReadOnlyDictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public bool IsValid => Value is not null;

    /// <summary>
    /// The errors in the mutable shape <c>TypedResults.ValidationProblem</c> expects.
    /// </summary>
    public Dictionary<string, string[]> ErrorsForProblemDetails()
    {
        return Errors.ToDictionary(e => e.Key, e => e.Value);
    }
}

/// <summary>
/// Factories for <see cref="ParseResult{T}"/>, kept on a non-generic type so
/// the value's type is inferred at the call site.
/// </summary>
public static class ParseResult
{
    public static ParseResult<T> Valid<T>(T value)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParseResult<T>(value, new Dictionary<string, string[]>());
    }

    public static ParseResult<T> Invalid<T>(IReadOnlyDictionary<string, string[]> errors)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("An invalid result needs at least one error.", nameof(errors));
        }

        return new ParseResult<T>(value: null, errors);
    }
}
