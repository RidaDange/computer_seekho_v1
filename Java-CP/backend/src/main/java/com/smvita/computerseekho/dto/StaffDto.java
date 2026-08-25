package com.smvita.computerseekho.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;

import java.math.BigDecimal;

/**
 * Deliberately has NO password/passwordHash field — Knowledge Base
 * decision (Turn 3, §9.1): staff credentials don't go through a plain
 * Table Maintenance grid edit. Creation generates a temporary password
 * server-side (see StaffService.create / StaffCreationResult); nothing
 * about a password ever round-trips through this DTO.
 */
public record StaffDto(
        Integer staffId,

        @NotBlank(message = "Name is required")
        String name,

        @NotBlank(message = "Email is required")
        @Email
        String email,

        String phone,

        @NotBlank(message = "Role is required")
        String role, // Admin | Counselor | Faculty | Manager | Receptionist

        String qualification,
        BigDecimal experience,
        String photoUrl,

        @NotBlank(message = "Username is required")
        String username,

        Boolean isActive
) {}
