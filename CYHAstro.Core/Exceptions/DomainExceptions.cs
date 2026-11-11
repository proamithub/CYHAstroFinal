namespace CYHAstro.Core.Exceptions;

/// <summary>
/// Base domain exception. Use for truly unexpected domain violations.
/// For expected business failures, use Result&lt;T&gt;.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.") { }
}

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access denied.")
        : base(message) { }
}

public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message = "A concurrency conflict occurred. Please retry.")
        : base(message) { }
}

public sealed class InsufficientBalanceException : DomainException
{
    public decimal Required { get; }
    public decimal Available { get; }

    public InsufficientBalanceException(decimal required, decimal available)
        : base($"Insufficient wallet balance. Required: {required:F2}, Available: {available:F2}")
    {
        Required = required;
        Available = available;
    }
}
