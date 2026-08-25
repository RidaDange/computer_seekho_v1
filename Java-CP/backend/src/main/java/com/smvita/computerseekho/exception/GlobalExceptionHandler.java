package com.smvita.computerseekho.exception;

import com.smvita.computerseekho.dto.ApiError;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;

import java.util.List;

/**
 * Backend requirement: "Global exceptions". Every controller in the app
 * relies on this single handler instead of try/catch blocks scattered
 * per-endpoint — new modules get consistent error responses for free.
 */
@RestControllerAdvice
public class GlobalExceptionHandler {

    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);

    /**
     * When true (dev profile), the exception type + message are echoed back
     * in the response so a 500 is debuggable straight from the browser's
     * network tab instead of being flattened to "An unexpected error
     * occurred". Off in prod — internal messages must never reach the
     * public site.
     */
    @Value("${app.expose-error-details:false}")
    private boolean exposeErrorDetails;

    @ExceptionHandler(ResourceNotFoundException.class)
    public ResponseEntity<ApiError> handleNotFound(ResourceNotFoundException ex) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND)
                .body(new ApiError(HttpStatus.NOT_FOUND.value(), ex.getMessage()));
    }

    @ExceptionHandler(BusinessRuleException.class)
    public ResponseEntity<ApiError> handleBusinessRule(BusinessRuleException ex) {
        return ResponseEntity.status(HttpStatus.UNPROCESSABLE_ENTITY)
                .body(new ApiError(HttpStatus.UNPROCESSABLE_ENTITY.value(), ex.getMessage()));
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<ApiError> handleValidation(MethodArgumentNotValidException ex) {
        List<String> fieldErrors = ex.getBindingResult().getFieldErrors().stream()
                .map(fe -> fe.getField() + ": " + fe.getDefaultMessage())
                .toList();
        return ResponseEntity.status(HttpStatus.BAD_REQUEST)
                .body(new ApiError(HttpStatus.BAD_REQUEST.value(), "Validation failed", fieldErrors));
    }

    /**
     * A path variable or query param that can't be converted to the
     * controller parameter's type — /courses/abc against
     * {@code @PathVariable Integer id}, or an unrecognised enum constant.
     * That's a client mistake (400), but with no explicit handler it fell
     * through to handleGeneral() below and surfaced as a misleading 500.
     */
    @ExceptionHandler(MethodArgumentTypeMismatchException.class)
    public ResponseEntity<ApiError> handleTypeMismatch(MethodArgumentTypeMismatchException ex) {
        String required = ex.getRequiredType() != null
                ? ex.getRequiredType().getSimpleName()
                : "the expected type";
        log.warn("Type mismatch on '{}': value '{}' is not a valid {}", ex.getName(), ex.getValue(), required);
        return ResponseEntity.status(HttpStatus.BAD_REQUEST)
                .body(new ApiError(HttpStatus.BAD_REQUEST.value(),
                        "'" + ex.getValue() + "' is not a valid value for '" + ex.getName()
                                + "' (expected " + required + ")."));
    }

    /**
     * Bad username/password, or a login attempt against a deactivated staff
     * account. Kept separate from the catch-all so it stays a 401 with a
     * usable message instead of a generic 500 — the Login screen renders
     * `message` directly.
     */
    @ExceptionHandler(org.springframework.security.core.AuthenticationException.class)
    public ResponseEntity<ApiError> handleAuthentication(
            org.springframework.security.core.AuthenticationException ex) {
        return ResponseEntity.status(HttpStatus.UNAUTHORIZED)
                .body(new ApiError(HttpStatus.UNAUTHORIZED.value(), ex.getMessage()));
    }

    /**
     * Authenticated, but this role isn't allowed here. Without an explicit
     * handler, an AccessDeniedException raised inside a controller/service
     * would be swallowed by handleGeneral() and reported as a 500.
     */
    @ExceptionHandler(org.springframework.security.access.AccessDeniedException.class)
    public ResponseEntity<ApiError> handleAccessDenied(
            org.springframework.security.access.AccessDeniedException ex) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN)
                .body(new ApiError(HttpStatus.FORBIDDEN.value(),
                        "Your role doesn't have access to this action."));
    }

    @ExceptionHandler(org.springframework.dao.DataIntegrityViolationException.class)
    public ResponseEntity<ApiError> handleDataIntegrity(org.springframework.dao.DataIntegrityViolationException ex) {
        // Catches DB-level constraint violations (e.g. a FK RESTRICT blocking
        // a delete, or a UNIQUE clash) that slip past app-level validation.
        log.warn("Data integrity violation", ex);
        return ResponseEntity.status(HttpStatus.CONFLICT)
                .body(new ApiError(HttpStatus.CONFLICT.value(),
                        "This action conflicts with existing data (e.g. a linked record still references it)."));
    }

    @ExceptionHandler(org.springframework.web.servlet.resource.NoResourceFoundException.class)
    public ResponseEntity<ApiError> handleNoRouteMatched(
            org.springframework.web.servlet.resource.NoResourceFoundException ex) {
        // Spring throws this (as of Spring Framework 6.1+) when a request
        // doesn't match any @RequestMapping. Handling it explicitly stops
        // it from falling into handleGeneral() below and showing a
        // confusing "unexpected error" for what's really just a typo'd or
        // wrong URL — e.g. hitting a frontend route against the backend port.
        return ResponseEntity.status(HttpStatus.NOT_FOUND)
                .body(new ApiError(HttpStatus.NOT_FOUND.value(),
                        "No endpoint found for this URL. Check the path and HTTP method."));
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<ApiError> handleGeneral(Exception ex) {
        // Log the full stack trace. Without this, an @ExceptionHandler bound
        // to Exception silences Spring's own error logging completely — every
        // unexpected failure becomes an untraceable 500 with nothing in the
        // console to work from.
        log.error("Unhandled exception while handling request", ex);

        String message = "An unexpected error occurred. Please try again.";
        List<String> details = null;
        if (exposeErrorDetails) {
            details = List.of(ex.getClass().getName() + ": " + ex.getMessage());
            message = message + " [" + ex.getClass().getSimpleName() + ": " + ex.getMessage() + "]";
        }

        return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(new ApiError(HttpStatus.INTERNAL_SERVER_ERROR.value(), message, details));
    }
}
