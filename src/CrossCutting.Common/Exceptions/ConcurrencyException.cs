namespace Domain.Exceptions;

/// <summary>
/// Exceção especializada para conflitos de concorrência no agregado.
/// </summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException() { }
    public ConcurrencyException(string? message) : base(message) { }
    public ConcurrencyException(string? message, Exception? inner) : base(message, inner) { }
}