namespace SmartTask.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.") { }
}

public class UnauthorizedDomainException : DomainException
{
    public UnauthorizedDomainException() : base("You are not authorized for this action.") { }
}
