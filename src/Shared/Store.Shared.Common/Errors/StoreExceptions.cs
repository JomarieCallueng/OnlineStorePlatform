namespace Store.Shared.Common.Errors;

/// <summary>404 — a requested resource does not exist.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>409 — the request conflicts with current state (e.g. duplicate, concurrency).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>400 — the request is invalid for a domain reason.</summary>
public sealed class StoreValidationException : Exception
{
    public StoreValidationException(string message) : base(message) { }
}
