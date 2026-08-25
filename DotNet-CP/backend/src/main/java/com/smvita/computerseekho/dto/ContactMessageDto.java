package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

import java.time.LocalDateTime;

public record ContactMessageDto(
        Integer messageId,

        @NotBlank(message = "Name is required")
        @Size(max = 150)
        String name,

        @NotBlank(message = "Email is required")
        @Email(message = "A valid email is required")
        @Size(max = 150)
        String email,

        @NotBlank(message = "Message is required")
        @Size(max = 500, message = "Message must be 500 characters or fewer")
        String message,

        Boolean isRead,
        LocalDateTime createdAt
) {}
