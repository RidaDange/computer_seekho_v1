package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record ClosureReasonDto(
        Integer reasonId,

        @NotBlank(message = "Reason text is required")
        @Size(max = 200)
        String reasonText,

        Boolean isActive
) {}
