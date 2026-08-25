namespace ComputerSeekho.Service;

/// <summary>
/// A business rule refused the action. Distinct from a validation failure:
/// the request was well-formed, the system just will not do it — a duplicate
/// email, a full batch, a short payment.
///
/// Maps to 422 Unprocessable Entity, matching the Java backend.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Nothing exists with that id. Maps to 404.</summary>
public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string resource, object id)
        : base($"{resource} {id} was not found") { }

    public ResourceNotFoundException(string message) : base(message) { }
}
