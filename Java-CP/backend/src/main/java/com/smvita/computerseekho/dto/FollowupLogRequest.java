package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;

import java.time.LocalDate;

/**
 * Logging the outcome of a follow-up attempt. `nextFollowup` is optional —
 * leaving it blank ends the follow-up trail, which is the normal path when
 * the counselor is about to close the enquiry instead.
 */
public record FollowupLogRequest(

        @NotBlank(message = "An outcome is required")
        String status, // "Done" or "No Response"

        String notes,

        LocalDate nextFollowup
) {}
