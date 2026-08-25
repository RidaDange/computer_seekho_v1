package com.smvita.computerseekho.dto;

import java.time.Instant;
import java.util.List;

/**
 * Uniform error shape for every non-2xx response. The frontend's
 * axiosClient interceptor reads `message` specifically — keep that field
 * name stable so every screen's error handling keeps working.
 */
public record ApiError(
        Instant timestamp,
        int status,
        String message,
        List<String> fieldErrors
) {
    public ApiError(int status, String message) {
        this(Instant.now(), status, message, null);
    }

    public ApiError(int status, String message, List<String> fieldErrors) {
        this(Instant.now(), status, message, fieldErrors);
    }
}
