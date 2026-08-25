namespace ComputerSeekho.DTO;

/// <summary>
/// The single error shape every failed request returns.
///
/// Matches the Java backend's ApiError field for field, because the shared
/// React client already parses it: axiosClient reads `message` and joins
/// `fieldErrors` onto it. Returning ASP.NET Core's default
/// ProblemDetails instead would leave every error on the frontend reading
/// "Something went wrong", since the property names differ.
/// </summary>
public record ApiError(
    int Status,
    string Message,
    IEnumerable<string>? FieldErrors = null
);
