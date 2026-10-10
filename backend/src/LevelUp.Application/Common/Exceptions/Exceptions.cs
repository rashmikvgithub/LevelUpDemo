namespace LevelUp.Application.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.")
{
    public string EntityName { get; } = entityName;
    public object Key { get; } = key;
}

/// <summary>Thrown when a request is valid in shape but violates a business rule. Mapped to HTTP 409.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
