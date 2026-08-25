package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

/**
 * Closing an enquiry. Both fields are required at the DTO level because the
 * KB (§3.2, Turn 4) made the reason mandatory whenever an enquiry closes
 * without converting — the service additionally rejects a reason that isn't
 * an active row in closure_reasons.
 */
public record CloseInquiryRequest(

        @NotBlank(message = "A closing status is required")
        String status, // "Lost" or "Not Interested"

        @NotNull(message = "A closure reason is required when closing an enquiry")
        Integer closureReasonId
) {}
