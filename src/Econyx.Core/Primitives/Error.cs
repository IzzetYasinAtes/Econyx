using System.Diagnostics.CodeAnalysis;

namespace Econyx.Core.Primitives;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static Error Validation(string message) => new("Error.Validation", message);
    public static Error NotFound(string entity, object id) => new("Error.NotFound", $"{entity} with id '{id}' was not found.");
    public static Error Conflict(string message) => new("Error.Conflict", message);
    public static Error Failure(string message) => new("Error.Failure", message);
}
